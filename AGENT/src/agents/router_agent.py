# src/agents/router_agent.py
# This file is now a specialized SQL router, not a general router.

import json
import os
from dotenv import load_dotenv

from langchain_google_vertexai import ChatVertexAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import JsonOutputParser

from database.Schema_map import SCHEMA_MAP 

# --- Load Environment Variables ---
load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))

# --- Service Account Key Authentication Setup ---
try:
    service_account_key_path = os.getenv("GOOGLE_APPLICATION_CREDENTIALS_PATH", r"C:\Users\Admin\Downloads\geminimcp-464809-eee97d96077e.json")
    
    if not os.path.exists(service_account_key_path):
        raise FileNotFoundError(f"Service account key file not found at: {service_account_key_path}")
    
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = service_account_key_path
    print(f"[SQLRouterAgent Setup] GOOGLE_APPLICATION_CREDENTIALS set to: {service_account_key_path}")

except FileNotFoundError as e:
    print(f"[SQLRouterAgent Fatal Error] {e}")
    print("Please ensure the service account JSON key file exists at the specified path.")
    raise
except Exception as e:
    print(f"[SQLRouterAgent Error] An unexpected error occurred during service account setup: {e}")
    raise

GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "geminimcp-464809")
GOOGLE_LOCATION = os.getenv("GOOGLE_LOCATION", "us-central1") 


class SQLRouterAgent:
    def __init__(self, model_name: str = "gemini-2.5-pro"):
        """
        Initializes the SQL Router Agent with a LangChain-compatible LLM
        from Google Vertex AI.
        """
        self.llm = ChatVertexAI(
            model_name=model_name,
            temperature=0.0,
            project=GCP_PROJECT_ID,
            location=GOOGLE_LOCATION
        )
        
        self.parser = JsonOutputParser()
        self.schema_map = SCHEMA_MAP
        
        self.prompt = ChatPromptTemplate.from_messages(
            [
                ("system", 
                 """
                 You are an expert SQL routing agent. Your task is to analyze a user's question, which is known to be a database query, and identify the specific tables and columns required to answer it.

                 Here is the database schema information you must use to identify the relevant tables and columns:
                 --- DATABASE SCHEMA CONTEXT ---
                 Schema Map (keywords to tables/columns/concepts):
                 {schema_map}

                 --- END DATABASE SCHEMA CONTEXT ---

                 Your goal is to identify ALL relevant table names and column names from the schema context provided that are needed to answer the user's question.
                 Prioritize selecting as few tables as possible to answer the question, but do not miss any that are required.

                 Your output MUST be a JSON object with the following structure:
                 {{
                     "tool": "SQL_AGENT", // This is fixed, as the primary router already decided this.
                     "relevant_tables": ["table1", "table2", ...],
                     "relevant_columns": ["column1", "column2", ...],
                     "reasoning": "Brief explanation for the table and column selection."
                 }}
                 
                 Ensure 'relevant_tables' and 'relevant_columns' are arrays of strings.
                 Do not include any other text or formatting outside of the JSON object.
                 """
                ),
                ("ai", "Okay, I understand my task is to find the relevant tables and columns for the user's database query. I will provide the results in the required JSON format:"),
                ("user", "{user_query}")
            ]
        ).partial(
            schema_map=json.dumps(self.schema_map, indent=2)
        )
        
        self.routing_chain = self.prompt | self.llm | self.parser

    async def route_query(self, user_query: str) -> dict:
        """
        Analyzes the user's query and identifies the relevant tables and columns.

        Args:
            user_query (str): The question asked by the user, which is a database query.

        Returns:
            dict: A dictionary containing the routing decision (tool, relevant_tables, relevant_columns, reasoning).
        """
        try:
            routing_decision = await self.routing_chain.ainvoke({"user_query": user_query})
            
            # The tool is now hardcoded to "SQL_AGENT" since the primary router has already made this decision.
            routing_decision["tool"] = "SQL_AGENT"
            
            expected_keys = ["tool", "relevant_tables", "relevant_columns", "reasoning"]
            if not all(key in routing_decision for key in expected_keys):
                raise ValueError("LLM response missing expected keys.")
            
            return routing_decision

        except Exception as e:
            print(f"Error identifying SQL components: {e}")
            return {"tool": "SQL_AGENT", "relevant_tables": [], "relevant_columns": [], "reasoning": f"Failed to identify SQL components: {e}. Cannot proceed with SQL generation."}