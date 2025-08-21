# src/agents/primary_router.py

import json
import os
from typing import Dict, Any, List, Optional
from dotenv import load_dotenv
import logging

from langchain_google_vertexai import ChatVertexAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import JsonOutputParser

# Import CRM tools for dynamic tool definitions
from agents.mcp_agent import mcp_tools

# --- Load environment variables ---
load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))

GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "geminimcp-464809")
GOOGLE_LOCATION = os.getenv("GOOGLE_LOCATION", "us-central1")

def get_crm_tool_definitions() -> List[Dict[str, str]]:
    """Dynamically gets the names and descriptions of available CRM tools."""
    global mcp_tools
    if mcp_tools is None:
        logging.warning("mcp_tools not yet initialized. Returning placeholder definitions.")
        return [
            {"name": "CRM_AGENT", "description": "Dynamic tool list not available yet."}
        ]
    
    return [{"name": tool.name, "description": tool.description} for tool in mcp_tools]

class PrimaryRouterAgent:
    """
    Enhanced router agent that handles visualization requests and routes queries to appropriate sub-agents.
    """
    def __init__(self, model_name: str = "gemini-2.5-pro"):
        self.llm = ChatVertexAI(
            model_name=model_name,
            temperature=0.0,
            project=GCP_PROJECT_ID,
            location=GOOGLE_LOCATION,
            max_output_tokens=2048
        )
        self.parser = JsonOutputParser()
        self.visualization_keywords = ["chart", "graph", "plot", "visualize", "pie", "bar", "line"]

        # Define all tool capabilities
        self.tool_definitions = {
            "SQL_ROUTER_AGENT": {
                "name": "SQL_ROUTER_AGENT",
                "description": (
                    "Use for questions requiring database analysis or querying. "
                    "Includes questions about sales, customers, products, leads, opportunities, etc."
                )
            },
            "CLARIFY_QUERY": {
                "name": "CLARIFY_QUERY",
                "description": (
                    "Use when request is ambiguous or missing required parameters like IDs. "
                    "Generates a question to get missing information."
                )
            },
            "VISUALIZATION_AGENT": {
                "name": "VISUALIZATION_AGENT",
                "description": (
                    "Use when user requests data visualization (charts/graphs). "
                    "Always used in conjunction with either CRM_AGENT or SQL_ROUTER_AGENT."
                )
            },
            "GENERAL_QUERY": {
                "name": "GENERAL_QUERY",
                "description": "Use for general questions not requiring specialized tools."
            },
            "CRM_AGENT": {
                "name": "CRM_AGENT",
                "description": "Handles queries that require interacting with the CRM system, like retrieving specific lead or opportunity details by ID. This agent should be the primary choice for direct record lookups. **However, if a specific CRM tool is not a perfect match, you can propose 'SQL_ROUTER_AGENT' as a fallback to query the database for a broader search.**"
            }
        }

        self._initialize_routing_chain()

    def _initialize_routing_chain(self):
        """Initialize the routing chain with dynamic tool definitions."""
        crm_tool_definitions = get_crm_tool_definitions()
        
        available_tools = (
            crm_tool_definitions + 
            [self.tool_definitions["CLARIFY_QUERY"]] +
            [self.tool_definitions["SQL_ROUTER_AGENT"]] +
            [self.tool_definitions["VISUALIZATION_AGENT"]] +
            [self.tool_definitions["GENERAL_QUERY"]]
        )

        # Update the prompt to include the fallback logic.
        self.prompt = ChatPromptTemplate.from_messages([
            ("system",
             """
             You are an expert routing agent that determines the best tool(s) for a user query.
             Follow this priority hierarchy and special cases:

             --- HIERARCHICAL PRIORITY ---
             1. CRM_AGENT: For specific CRM tool operations (e.g., retrieving a single record by ID).
             2. CLARIFY_QUERY: For ambiguous/missing parameter requests.
             3. SQL_ROUTER_AGENT: For database queries/analysis.
             4. GENERAL_QUERY: For all other cases.

             --- SPECIAL CASES ---
             * CRM_AGENT and SQL_ROUTER_AGENT Fallback:
               - If a query seems to be for a CRM agent but is a general query (e.g., "list all opportunities" instead of "show me opportunity OPP001"), consider `SQL_ROUTER_AGENT` as the primary tool.
               - If a query is for a specific CRM record by ID, but there's a chance the record might not exist or the CRM tool might fail, you can suggest `SQL_ROUTER_AGENT` as a `fallback_tool`.

             * Visualization requests (terms like {visualization_keywords}):
               - If the primary tool is a data tool (CRM or SQL), add `VISUALIZATION_AGENT` as a `secondary_tool`.

             --- AVAILABLE TOOLS ---
             {available_tools}

             Respond with JSON containing:
             - tool_name: Primary tool (required)
             - secondary_tool: Optional tool (for visualization)
             - fallback_tool: Optional tool to use if the primary tool fails.
             - reasoning: Explanation of decision
             """
            ),
            ("user", "{user_query}")
        ]).partial(
            available_tools=json.dumps(available_tools, indent=2),
            visualization_keywords=", ".join(f"'{kw}'" for kw in self.visualization_keywords)
        )

        self.routing_chain = self.prompt | self.llm | self.parser

    async def route_query(self, user_query: str) -> Dict[str, Any]:
        """
        Routes user queries with support for visualization requests.
        Returns dict with:
        - tool_name: Primary tool
        - secondary_tool: Optional secondary tool (e.g., VISUALIZATION_AGENT)
        - fallback_tool: Optional fallback tool (e.g., SQL_ROUTER_AGENT)
        - reasoning: Explanation of routing decision
        """
        try:
            routing_decision = await self.routing_chain.ainvoke({"user_query": user_query})
            
            if not isinstance(routing_decision, dict):
                routing_decision = {"tool_name": "GENERAL_QUERY", "reasoning": "Invalid routing decision format"}
            
            # This logic should be handled by the LLM's prompt now.
            # We can still keep the explicit check to be safe.
            if self._is_visualization_request(user_query):
                routing_decision.setdefault("secondary_tool", "VISUALIZATION_AGENT")
            
            return routing_decision
            
        except Exception as e:
            logging.error(f"Error during primary routing: {e}")
            return {
                "tool_name": "GENERAL_QUERY",
                "reasoning": f"Error during routing: {e}",
                "error": str(e)
            }

    def _is_visualization_request(self, query: str) -> bool:
        """Check if the query contains visualization-related keywords."""
        query_lower = query.lower()
        return any(keyword in query_lower for keyword in self.visualization_keywords)