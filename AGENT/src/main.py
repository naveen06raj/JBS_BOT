import os
import asyncio
import pandas as pd
from typing import TypedDict, Dict, Any, List, Optional
from dotenv import load_dotenv
import logging
from fastapi import FastAPI, HTTPException, status
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from decimal import Decimal
from fastapi.encoders import jsonable_encoder
from typing import Annotated

from fastapi.middleware.cors import CORSMiddleware

# LangChain / LangGraph imports
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser
from langchain_google_vertexai import ChatVertexAI
from langgraph.graph import StateGraph, END
from langchain_core.messages import BaseMessage

# Import custom modules
from agents.primary_router import PrimaryRouterAgent
from agents.router_agent import SQLRouterAgent
from agents.sql_agent import SQLAgent
from agents.mcp_agent import setup_agent_for_ui, invoke_agent_with_history, mcp_tools
from agents.visualization_agent import VisualizationAgent
from database.db_connector import DatabaseConnector
from database.Schema_full import fetch_full_schema_dataframe
from utils.schema_updater import update_schema_map_file, reload_schema_map_module
from utils.schema_comparer import get_refined_schema_for_llm

# --- Setup Logging ---
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# --- Load Environment Variables ---
load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), 'config', '.env'))

# --- Service Account Key Authentication Setup ---
try:
    service_account_key_path = os.getenv("GOOGLE_APPLICATION_CREDENTIALS_PATH", r"C:\Users\Admin\Downloads\geminimcp-464809-eee97d96077e.json")
    
    if not os.path.exists(service_account_key_path):
        raise FileNotFoundError(f"Service account key file not found at: {service_account_key_path}")
    
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = service_account_key_path
    logging.info(f"GOOGLE_APPLICATION_CREDENTIALS set to: {service_account_key_path}")

except FileNotFoundError as e:
    logging.error(f"Fatal Error: {e}")
    logging.error("Please ensure the service account JSON key file exists at the specified path in your .env or script.")
    raise
except Exception as e:
    logging.error(f"An unexpected error occurred during service account setup: {e}")
    raise

GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "geminimcp-464809")
GOOGLE_LOCATION = os.getenv("GOOGLE_LOCATION", "us-central1")

# --- Helper Function for Markdown Table Formatting ---
def format_results_to_markdown_table(sql_results: List[Dict[str, Any]]) -> str:
    """Converts a list of dictionaries (SQL results) into a Markdown table string."""
    if not sql_results:
        return "No data found."
    
    # Use pandas to easily create the table, then convert to a Markdown string
    try:
        df = pd.DataFrame(sql_results)
        
        # Get column headers
        headers = " | ".join(df.columns)
        
        # Create the separator line
        separator = " | ".join(['---'] * len(df.columns))
        
        # Create the data rows
        rows = []
        for _, row in df.iterrows():
            row_values = [str(row[col]) for col in df.columns]
            rows.append(" | ".join(row_values))
            
        # Join everything together
        markdown_table = f"| {headers} |\n| {separator} |\n" + "\n".join([f"| {row} |" for row in rows])
        
        return markdown_table
    except Exception as e:
        logging.error(f"Error formatting SQL results to Markdown table: {e}")
        return f"Could not display results. Error: {e}"

# --- FastAPI App ---
app = FastAPI(
    title="Text-to-SQL & CRM API",
    description="API for multi-agent query processing with visualization",
    version="1.0.0"
)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Pydantic Models ---
class QueryRequest(BaseModel):
    query: str
    include_sql: Optional[bool] = False
    include_results: Optional[bool] = False
    include_visualization: Optional[bool] = True

class QueryResponse(BaseModel):
    response: str
    sql_query: Optional[str] = None
    sql_results: Optional[List[Dict[str, Any]]] = None
    error: Optional[str] = None
    success: bool
    chart_image_base64: Optional[str] = None

# --- Global Instances ---
db_connector = DatabaseConnector()
primary_router = PrimaryRouterAgent()
sql_router = SQLRouterAgent()
sql_agent = SQLAgent(db_connector)
visualization_agent = VisualizationAgent()

# LLM for general responses and final answer generation
final_response_llm = ChatVertexAI(
    model_name="gemini-2.5-pro",
    temperature=0.0,
    project=GCP_PROJECT_ID,
    location=GOOGLE_LOCATION
)
final_response_parser = StrOutputParser()

final_response_prompt = ChatPromptTemplate.from_messages(
    [
        ("system",
         """
         You are an AI assistant designed to provide concise and helpful answers.
         Based on the context provided, generate a natural language response.

         **Instructions:**
         - If `SQL Results` are available, they will be provided as a pre-formatted **Markdown table** or a **bulleted list**.
         - Your task is to incorporate this formatted text into your response directly without modification.
         - If no data is found, state "No data found".
         - If an `Error Message` is provided, explain the issue politely.
         - For general non-database queries, answer directly.
         - Be concise and professional.

         Context:
         User Query: {user_query}
         SQL Query (if generated): {sql_query}
         SQL Results (if available): {sql_results}
         Error Message (if any): {error_message}
         """
        ),
        ("human", "Generate a natural language response based on the above context.")
    ]
)
final_response_chain = final_response_prompt | final_response_llm | final_response_parser

general_query_prompt = ChatPromptTemplate.from_messages(
    [
        ("system", "You are a helpful AI assistant. Answer the user's question directly and concisely."),
        ("human", "{user_query}")
    ]
)
general_query_chain = general_query_prompt | final_response_llm | final_response_parser

clarify_prompt = ChatPromptTemplate.from_messages(
    [
        ("system",
         """
         You are a helpful assistant designed to ask clarifying questions.
         Based on the user's query and the reason provided, generate a concise, polite question.
         Do not provide any other information or context. Just ask the question.
         
         User Query: {user_query}
         Reason for ambiguity: {reasoning}
         """
        ),
        ("human", "What is the clarifying question?")
    ]
)
clarify_chain = clarify_prompt | final_response_llm | final_response_parser

# --- LangGraph State Definition ---
class GraphState(TypedDict):
    user_query: str
    routing_decision: Dict[str, Any]
    sql_query: str
    sql_results: List[Dict[str, Any]]
    final_response: str
    error_message: Annotated[List[str], "error_messages"]
    db_schema_df: pd.DataFrame
    relevant_db_schema_df: pd.DataFrame
    chat_history: List[BaseMessage]
    visualization_data: Optional[Dict[str, Any]]

# --- LangGraph Nodes ---
async def primary_route_node(state: GraphState) -> Dict[str, Any]:
    """Node for the high-level router to decide which sub-agent to use."""
    logging.info(f"NODE: primary_route_node - User Query: {state['user_query']}")
    try:
        routing_decision = await primary_router.route_query(state['user_query'])
        logging.info(f"NODE: primary_route_node - Routing Decision: {routing_decision}")
        return {"routing_decision": routing_decision, "error_message": ""}
    except Exception as e:
        logging.error(f"NODE: primary_route_node - Error routing query: {e}", exc_info=True)
        return {"error_message": f"An error occurred during query routing: {e}"}

async def clarify_query_node(state: GraphState) -> Dict[str, Any]:
    """Node to handle clarification requests."""
    logging.info(f"NODE: clarify_query_node - Handling clarification request.")
    routing_decision = state.get("routing_decision", {})
    reasoning = routing_decision.get("reasoning", "The query was ambiguous and requires more information.")
    
    try:
        clarifying_question = await clarify_chain.ainvoke({
            "user_query": state["user_query"],
            "reasoning": reasoning
        })
        return {"final_response": clarifying_question, "error_message": ""}
    except Exception as e:
        logging.error(f"NODE: clarify_query_node - Error generating clarifying question: {e}", exc_info=True)
        return {"final_response": "I'm sorry, your request requires more specific information. Could you please provide additional details?", "error_message": f"Error in clarification generation: {e}"}

async def sql_route_node(state: GraphState) -> Dict[str, Any]:
    """Node to use the specialized SQL Router to get relevant tables/columns."""
    logging.info(f"NODE: sql_route_node - Using specialized SQL Router.")
    try:
        sql_routing_decision = await sql_router.route_query(state['user_query'])
        return {"routing_decision": sql_routing_decision, "error_message": ""}
    except Exception as e:
        logging.error(f"NODE: sql_route_node - Error from SQL Router: {e}", exc_info=True)
        return {"error_message": f"An error occurred in the SQL routing step: {e}"}

async def generate_sql_node(state: GraphState) -> Dict[str, Any]:
    """Node to generate SQL query using the SQLAgent."""
    logging.info(f"NODE: generate_sql_node - Generating SQL...")
    user_query = state['user_query']
    routing_decision = state.get('routing_decision', {})
    relevant_tables = routing_decision.get('relevant_tables', [])
    relevant_columns = routing_decision.get('relevant_columns', [])

    try:
        sql_query, relevant_schema_df = await sql_agent.generate_sql_query(
            user_query, relevant_tables, relevant_columns
        )
        
        if "Error:" in sql_query:
            logging.error(f"NODE: generate_sql_node - SQL generation failed: {sql_query}")
            return {"sql_query": "", "relevant_db_schema_df": pd.DataFrame(),
                     "error_message": sql_query.replace("Error: ", "")}
        
        logging.info(f"NODE: generate_sql_node - Generated SQL: {sql_query}")
        return {"sql_query": sql_query, "relevant_db_schema_df": relevant_schema_df, "error_message": ""}
    except Exception as e:
        logging.error(f"NODE: generate_sql_node - Error generating SQL: {e}", exc_info=True)
        return {"error_message": f"An error occurred during SQL generation: {e}"}

async def execute_sql_node(state: GraphState) -> Dict[str, Any]:
    """Node to execute the generated SQL query using DatabaseConnector."""
    logging.info(f"NODE: execute_sql_node - Executing SQL...")
    sql_query = state['sql_query']

    if not sql_query:
        logging.warning("NODE: execute_sql_node - No SQL query provided for execution.")
        return {"sql_results": [], "error_message": "No SQL query was generated to execute."}

    if any(sql_query.strip().upper().startswith(kw) for kw in ["DELETE", "UPDATE", "INSERT", "CREATE", "ALTER", "DROP", "TRUNCATE"]):
        logging.warning(f"NODE: execute_sql_node - Attempted execution of forbidden SQL: {sql_query}")
        return {"sql_results": [], "error_message": "SQL query contains forbidden operations. Execution denied for safety."}
        
    try:
        sql_results = await db_connector.execute_query(sql_query, fetch=True)
        logging.info(f"NODE: execute_sql_node - SQL Results Count: {len(sql_results) if sql_results else 0}")
        return {"sql_results": sql_results, "error_message": ""}
    except Exception as e:
        logging.error(f"NODE: execute_sql_node - Error executing SQL: {e}", exc_info=True)
        return {"sql_results": [], "error_message": f"An error occurred during SQL execution: {e}"}

# In your main script, replace the existing call_crm_agent_node with this version.
async def call_crm_agent_node(state: GraphState) -> Dict[str, Any]:
    """Node to invoke the CRM agent and check for failure messages."""
    logging.info(f"NODE: call_crm_agent_node - Calling CRM Agent with query: {state['user_query']}")
    try:
        crm_agent_app, _ = await setup_agent_for_ui()
        final_response_content = await invoke_agent_with_history(
            agent_app_instance=crm_agent_app, 
            user_question=state['user_query'], 
            chat_history=state.get('chat_history', [])
        )
        
        # Check for generic failure messages from the CRM agent itself.
        if "I am sorry" in final_response_content or "I couldn't" in final_response_content or "error" in final_response_content or "technical error" in final_response_content or "error occurred" in final_response_content:
             # Return a failure message that can be used by the conditional edge
             return {"error_message": final_response_content}
        
        # If the response is not a known error message, it's a success
        return {"final_response": final_response_content}
        
    except Exception as e:
        logging.error(f"NODE: call_crm_agent_node - Error calling CRM agent: {e}", exc_info=True)
        # Return the exception message to trigger the fallback
        return {"error_message": f"An error occurred while using the CRM agent: {e}"}
    
async def visualization_node(state: GraphState) -> Dict[str, Any]:
    """Node to generate visualizations if requested."""
    logging.info(f"NODE: visualization_node - Checking for visualization request")
    
    # Check if user asked for visualization
    query_lower = state["user_query"].lower()
    visualization_keywords = ["chart", "graph", "plot", "visualize", "pie", "bar", "line"]
    
    if not any(keyword in query_lower for keyword in visualization_keywords):
        logging.info("NODE: visualization_node - No visualization requested")
        return {"visualization_data": None}
    
    # Get the data to visualize
    data = state.get("sql_results", [])
    if not data:
        logging.info("NODE: visualization_node - No data available for visualization")
        return {"visualization_data": None}
    
    try:
        visualization = await visualization_agent.generate_visualization(
            state["user_query"], 
            data
        )
        if "error" in visualization:
            logging.warning(f"NODE: visualization_node - {visualization['error']}")
            return {"visualization_data": None}
        return {"visualization_data": visualization}
    except Exception as e:
        logging.error(f"NODE: visualization_node - Error generating visualization: {e}")
        return {"error_message": f"Visualization error: {e}", "visualization_data": None}

async def generate_final_response_node(state: GraphState) -> Dict[str, Any]:
    """Node to generate the final natural language response to the user."""
    logging.info(f"NODE: generate_final_response_node - Generating final response...")
    
    visualization = state.get("visualization_data")
    chart_image_base64 = visualization.get("image_base64") if visualization else None
    
    try:
        if state.get("final_response"):
            response = state["final_response"]
        else:
            # --- START of changed code ---
            # Pre-format the SQL results into a Markdown table string
            formatted_sql_results = ""
            if state.get('sql_results'):
                formatted_sql_results = format_results_to_markdown_table(state['sql_results'])

            # Generate response with the pre-formatted string
            response = await final_response_chain.ainvoke({
                "user_query": state['user_query'],
                "sql_query": state.get('sql_query', ''),
                "sql_results": formatted_sql_results,
                "error_message": state.get('error_message', '')
            })
            # --- END of changed code ---
        
        logging.info(f"NODE: generate_final_response_node - Final Response: {response}")
        return {"final_response": response, "error_message": "", "visualization_data": {"chart_image_base64": chart_image_base64}}
    except Exception as e:
        logging.error(f"NODE: generate_final_response_node - Error generating final response: {e}")
        return {"final_response": "I apologize, but I encountered an internal error while trying to formulate a response.", "error_message": f"Error in final response generation: {e}", "visualization_data": None}

async def general_query_response_node(state: GraphState) -> Dict[str, Any]:
    """Node to handle general (non-database) queries."""
    logging.info(f"NODE: general_query_response_node - Handling general query...")
    try:
        response = await general_query_chain.ainvoke({"user_query": state['user_query']})
        logging.info(f"NODE: general_query_response_node - General Response: {response}")
        return {"final_response": response, "error_message": "", "sql_query": "", "sql_results": [], "visualization_data": None}
    except Exception as e:
        logging.error(f"NODE: general_query_response_node - Error handling general query: {e}", exc_info=True)
        return {"final_response": "I'm sorry, I couldn't process your general question due to an error.", "error_message": f"Error in general query response: {e}", "visualization_data": None}

async def handle_error_node(state: GraphState) -> Dict[str, Any]:
    """Node to consolidate and present errors."""
    logging.error(f"NODE: handle_error_node - Handling error: {state.get('error_message', 'Unknown error')}")
    user_facing_error = (
        "I encountered an issue while trying to answer your question. "
        f"Details: {state.get('error_message', 'An unexpected error occurred.')}"
        "\nPlease try rephrasing your question or contact support if the problem persists."
    )
    return {"final_response": user_facing_error, "error_message": state.get('error_message'), "visualization_data": None}

# --- LangGraph Conditional Edges Logic ---
def primary_route_decision(state: GraphState) -> str:
    """Decides which agent to use based on the high-level router's output."""
    routing_decision = state.get("routing_decision", {})
    tool_name = routing_decision.get("tool_name")
    error = state.get("error_message")

    if error:
        return "handle_error"
    elif tool_name == "SQL_ROUTER_AGENT":
        return "sql_route_node"
    elif tool_name == "CRM_AGENT":
        return "call_crm_agent"
    elif tool_name == "CLARIFY_QUERY":
        return "clarify_query_node"
    elif tool_name == "GENERAL_QUERY":
        return "general_response"
    else:
        return "handle_error"

def sql_route_to_sql_generation(state: GraphState) -> str:
    """Checks if the SQL router successfully found tables/columns."""
    routing_decision = state.get("routing_decision", {})
    relevant_tables = routing_decision.get("relevant_tables", [])
    error = state.get("error_message")
    
    if error or not relevant_tables:
        return "handle_error"
    else:
        return "generate_sql"

def check_for_error(state: GraphState) -> str:
    """Generic check to transition to error handler if an error message is present in state."""
    if state.get("error_message"):
        return "handle_error"
    return "continue"
# In your main script, replace the existing crm_fallback_decision with this version.
def crm_fallback_decision(state: GraphState) -> str:
    """Decides whether to fallback to SQL based on the CRM agent's result."""
    error = state.get("error_message")
    routing_decision = state.get("routing_decision", {})
    fallback_tool = routing_decision.get("fallback_tool")
    
    # If the CRM agent failed and a fallback is defined...
    if error and fallback_tool == "SQL_ROUTER_AGENT":
        # Log the fallback decision
        logging.warning("NODE: crm_fallback_decision - CRM agent failed, falling back to SQL.")
        
        # We need to re-route the query to the SQL path.
        # The next node needs the `routing_decision` to be for SQL.
        # Clear the error message so the SQL path doesn't immediately fail.
        # CRITICAL: We update the state in-place to avoid the concurrency issue.
        state["routing_decision"]["tool_name"] = "SQL_ROUTER_AGENT"
        state["routing_decision"]["reasoning"] = "Fallback from CRM agent failure."
        state["error_message"] = "" 
        
        return "sql_route_node"
    
    # If there's an error but no fallback, or a different fallback, go to error handler.
    if error:
        return "handle_error"
        
    # If the CRM call was successful, continue to the visualization node.
    return "visualization_node"

# --- LangGraph Workflow Definition ---
workflow = StateGraph(GraphState)

# Add nodes
workflow.add_node("primary_route_node", primary_route_node)
workflow.add_node("clarify_query_node", clarify_query_node)
workflow.add_node("sql_route_node", sql_route_node)
workflow.add_node("generate_sql", generate_sql_node)
workflow.add_node("execute_sql", execute_sql_node)
workflow.add_node("call_crm_agent", call_crm_agent_node)
workflow.add_node("visualization_node", visualization_node)
workflow.add_node("generate_final_response", generate_final_response_node)
workflow.add_node("general_response", general_query_response_node)
workflow.add_node("handle_error", handle_error_node)

# Set the entry point
workflow.set_entry_point("primary_route_node")

# Add conditional edges
workflow.add_conditional_edges(
    "primary_route_node",
    primary_route_decision,
    {
        "sql_route_node": "sql_route_node",
        "call_crm_agent": "call_crm_agent",
        "clarify_query_node": "clarify_query_node",
        "general_response": "general_response",
        "handle_error": "handle_error"
    }
)

workflow.add_edge("clarify_query_node", END)

workflow.add_conditional_edges(
    "sql_route_node",
    sql_route_to_sql_generation,
    {
        "generate_sql": "generate_sql",
        "handle_error": "handle_error"
    }
)

workflow.add_conditional_edges(
    "generate_sql",
    check_for_error,
    {
        "continue": "execute_sql",
        "handle_error": "handle_error"
    }
)

workflow.add_conditional_edges(
    "execute_sql",
    check_for_error,
    {
        "continue": "visualization_node",
        "handle_error": "handle_error"
    }
)
workflow.add_conditional_edges(
    "call_crm_agent",
    crm_fallback_decision,
    {
        "sql_route_node": "sql_route_node", # The new fallback path
        "visualization_node": "visualization_node",
        "handle_error": "handle_error"
    }
)

workflow.add_edge("call_crm_agent", "visualization_node")
workflow.add_edge("visualization_node", "generate_final_response")
workflow.add_edge("general_response", "generate_final_response")
workflow.add_edge("generate_final_response", END)
workflow.add_edge("handle_error", END)

# Compile the graph
text_to_sql_app = workflow.compile()

# --- Initial Application Setup ---
@app.on_event("startup")
async def initial_app_setup():
    """Performs initial setup tasks."""
    logging.info("Starting initial application setup...")
    global sql_router
    
    # Setup for SQL agent
    success = await update_schema_map_file(db_connector)
    if success:
        logging.info("Schema map file updated successfully.")
    else:
        logging.error("Failed to update schema map file.")
    
    try:
        reloaded_schema_map = reload_schema_map_module()
        sql_router = SQLRouterAgent()
        logging.info("SQLRouterAgent reloaded with updated SCHEMA_MAP.")
    except Exception as e:
        logging.error(f"Failed to reload Schema_map module: {e}", exc_info=True)
    
    # Setup for the CRM agent
    try:
        await setup_agent_for_ui()
        logging.info("MCP CRM agent tools initialized.")
    except Exception as e:
        logging.error(f"Failed to initialize MCP CRM agent tools: {e}", exc_info=True)
        
    logging.info("Initial application setup complete.")

# --- API Endpoints ---
@app.post("/query", response_model=QueryResponse)
async def process_query(request: QueryRequest):
    initial_state: GraphState = {
        "user_query": request.query,
        "routing_decision": {},
        "sql_query": "",
        "sql_results": [],
        "final_response": "",
        "error_message": "",
        "db_schema_df": pd.DataFrame(),
        "relevant_db_schema_df": pd.DataFrame(),
        "chat_history": [],
        "visualization_data": None
    }

    try:
        final_state = await text_to_sql_app.ainvoke(initial_state)
        
        chart_image_base64 = None
        if final_state.get("visualization_data") and "chart_image_base64" in final_state["visualization_data"]:
            chart_image_base64 = final_state["visualization_data"]["chart_image_base64"]

        response_data = jsonable_encoder({
            "response": final_state.get("final_response", "No response generated."),
            "success": not bool(final_state.get("error_message")),
            "error": final_state.get("error_message"),
            "sql_query": final_state.get("sql_query") if request.include_sql else None,
            "sql_results": final_state.get("sql_results", []) if request.include_results else None,
            "chart_image_base64": chart_image_base64 if request.include_visualization else None
        }, custom_encoder={Decimal: float})

        return JSONResponse(content=response_data)
    
    except Exception as e:
        logging.error(f"Error processing query: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"An error occurred while processing your query: {str(e)}"
        )

@app.get("/health")
async def health_check():
    """Health check endpoint for the API"""
    try:
        await db_connector.execute_query("SELECT 1", fetch=True)
        return {"status": "healthy", "database": "connected"}
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=f"Service unavailable: Database connection failed - {str(e)}"
        )

# --- Main Execution ---
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8004)