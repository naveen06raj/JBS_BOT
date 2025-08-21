# src/agents/sql_agent.py

import pandas as pd
from typing import List, Dict, Any, Tuple
import logging
import os
from dotenv import load_dotenv

from langchain_google_vertexai import ChatVertexAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser

from database.db_connector import DatabaseConnector
from database.Schema_full import fetch_full_schema_dataframe
from utils.schema_comparer import get_refined_schema_for_llm 


# Setup logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# --- Load Environment Variables ---
load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))

# --- Service Account Key Authentication Setup ---
try:
    service_account_key_path = r"C:\Users\Admin\Downloads\geminimcp-464809-eee97d96077e.json"
    
    if not os.path.exists(service_account_key_path):
        raise FileNotFoundError(f"Service account key file not found at: {service_account_key_path}")
    
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = service_account_key_path
    print(f"[RouterAgent Setup] GOOGLE_APPLICATION_CREDENTIALS set to: {service_account_key_path}")

except FileNotFoundError as e:
    print(f"[RouterAgent Fatal Error] {e}")
    print("Please ensure the service account JSON key file exists at the specified path.")
    raise
except Exception as e:
    print(f"[RouterAgent Error] An unexpected error occurred during service account setup: {e}")
    raise

# These global variables will be used directly in ChatVertexAI init
GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "geminimcp-464809")
GOOGLE_LOCATION = os.getenv("GOOGLE_LOCATION", "us-central1") 


class SQLAgent:
    def __init__(self, db_connector: DatabaseConnector):
        """
        Initializes the SQL Agent with a database connector and LangChain-compatible LLM.
        The LLM parameters (model, project, location) are hardcoded internally.
        """
        self.db_connector = db_connector
        self.llm = ChatVertexAI(
            model_name="gemini-2.5-pro", # Hardcoded model name
            temperature=0.0,
            project="geminimcp-464809", # Hardcoded project ID
            location="us-central1" # Hardcoded location
        )
        self.parser = StrOutputParser()

        self.prompt_template = ChatPromptTemplate.from_messages(
            [
                ("system", 
                 """
                 You are an expert PostgreSQL SQL query generator. Your task is to translate natural language questions into accurate and efficient SQL queries.
                 You will be provided with a user's question and a highly relevant subset of the database schema.
                 
                 --- RELEVANT DATABASE SCHEMA CONTEXT ---
                 {formatted_schema}
                 --- END RELEVANT DATABASE SCHEMA CONTEXT ---

                 Instructions for SQL Generation:
                 1.  *NEVER generate SQL queries that include DELETE, UPDATE, INSERT, CREATE, ALTER, DROP, or TRUNCATE statements.**
                 2.  **Strictly use only the tables and columns provided in the SCHEMA CONTEXT.** Do not invent tables or columns.
                 3.  **Identify and use appropriate JOINs.** If tables are related by Foreign Keys (FKs), use explicit JOINs (e.g., `INNER JOIN` or `LEFT JOIN` as appropriate) to connect them. The schema context will clearly show Primary Key (PRI) and Foreign Key (MUL) relationships.
                 4.  **Be mindful of column data types.** For example, use `DATE_TRUNC`, `CAST`, or specific date/time functions for date comparisons, `ILIKE` for case-insensitive string matching, and aggregate functions (`SUM`, `COUNT`, `AVG`, `MAX`, `MIN`) when appropriate.
                 5.  **Handle ambiguous column names.** If two or more tables have the same column name (e.g., 'id'), always qualify them with the table alias (e.g., `t1.id`, `t2.id`). Assign distinct, short aliases to each table used in the query.
                 6.  **Use LIMIT clause** if the user asks for "top N", "first N", or similar limited results.
                 7.  **Avoid using subqueries** unless absolutely necessary for complex aggregations or specific analytical needs; prefer JOINs for simpler relationships.
                 8.  **Return ONLY the SQL query string.** Do not include any other text, explanations, or formatting like markdown backticks.
                 9.  If a column contains string values that represent categories or names, use exact matches with single quotes or `ILIKE` as needed (e.g., `WHERE status = 'Open'` or `WHERE customer_name ILIKE '%John%'`).
                 10. When filtering by dates, ensure the date format in your `WHERE` clause matches typical PostgreSQL format (e.g., 'YYYY-MM-DD'). Use `BETWEEN 'YYYY-MM-DD' AND 'YYYY-MM-DD'` for date ranges.
                 11. If asked for a count, sum, or average, use the appropriate aggregate function and a `GROUP BY` clause if grouping by other columns. Alias the aggregate result (e.g., `COUNT(*) AS total_items`).
                 12. If the user asks for specific columns, select only those, but include necessary join columns.

                 SQL Query:
                 """
                ),
                ("human", "{user_query}") 
            ]
        )
        self.sql_chain = self.prompt_template | self.llm | self.parser

    def _prune_and_format_schema_for_llm(self, relevant_schema_df: pd.DataFrame) -> str:
        """
        Formates the relevant schema DataFrame into a concise string for the LLM,
        including table names, columns with types/keys, and explicit FK relationships.
        This is the "Prune and Format Schema for LLM" step.
        """
        if relevant_schema_df.empty:
            return "No relevant schema information found for SQL generation."

        formatted_output = []
        for table_name in relevant_schema_df['TABLE_NAME'].unique():
            table_df = relevant_schema_df[relevant_schema_df['TABLE_NAME'] == table_name]
            
            formatted_output.append(f"Table: {table_name}")
            
            columns_info = []
            for _, row in table_df.iterrows():
                col_name = row['COLUMN_NAME']
                data_type = row['DATA_TYPE']
                column_key = row['COLUMN_KEY'] # 'PRI', 'MUL', or ''

                col_detail = f"{col_name} ({data_type}"
                if column_key:
                    col_detail += f", {column_key}"
                col_detail += ")"
                columns_info.append(col_detail)
            
            formatted_output.append(f"Columns: {', '.join(columns_info)}")

            fks_from_this_table = table_df[table_df['COLUMN_KEY'] == 'MUL']
            if not fks_from_this_table.empty:
                fk_lines = []
                for _, fk_row in fks_from_this_table.drop_duplicates(subset=['COLUMN_NAME', 'REFERENCED_TABLE_NAME', 'REFERENCED_COLUMN_NAME']).iterrows():
                    if fk_row['REFERENCED_TABLE_NAME'] and fk_row['REFERENCED_COLUMN_NAME']:
                        fk_lines.append(
                            f"  FOREIGN KEY ({fk_row['COLUMN_NAME']}) REFERENCES "
                            f"{fk_row['REFERENCED_TABLE_NAME']}({fk_row['REFERENCED_COLUMN_NAME']})"
                        )
                if fk_lines:
                    formatted_output.append("Relationships (from this table):")
                    formatted_output.extend(fk_lines)
            
            formatted_output.append("")

        return "\n".join(formatted_output).strip()


    async def generate_sql_query(self, user_query: str, relevant_tables: List[str], relevant_columns: List[str]) -> Tuple[str, pd.DataFrame]:
        """
        Generates a SQL query based on the user's question and relevant schema hints.
        """
        logging.info(f"SQLAgent received query: '{user_query}'")
        logging.info(f"RouterAgent hints - Relevant Tables: {relevant_tables}, Relevant Columns: {relevant_columns}")
        
        try:
            full_schema_df = await fetch_full_schema_dataframe(self.db_connector)
            if full_schema_df.empty:
                logging.error("Failed to fetch full schema from database. Cannot generate SQL.")
                return "Error: Could not retrieve database schema.", pd.DataFrame()

            relevant_and_refined_schema_df = await get_refined_schema_for_llm(
                full_schema_df, relevant_tables, relevant_columns
            )
            
            if relevant_and_refined_schema_df.empty:
                logging.warning("No relevant schema information found after refinement. Cannot generate meaningful SQL.")
                return "Error: No relevant schema found for your query. Please rephrase or check database configuration.", pd.DataFrame()

            formatted_schema_for_llm = self._prune_and_format_schema_for_llm(relevant_and_refined_schema_df)
            logging.info(f"Formatted schema sent to LLM:\n---\n{formatted_schema_for_llm}\n---")

            sql_query = await self.sql_chain.ainvoke({
                "formatted_schema": formatted_schema_for_llm,
                "user_query": user_query
            })
            
            sql_query = sql_query.replace("```sql", "").replace("```", "").strip()

            logging.info(f"Generated SQL Query: \n{sql_query}")
            return sql_query, relevant_and_refined_schema_df

        except Exception as e:
            logging.error(f"Error in SQLAgent.generate_sql_query: {e}", exc_info=True)
            return f"Error generating SQL query: {e}", pd.DataFrame()

# --- Test block for SQLAgent.py ---
# if __name__ == "__main__":
#     import asyncio 
#     import os
#     from dotenv import load_dotenv

#     async def run_sql_agent_tests():
#         print("--- Running SQLAgent tests ---")
        
#         # Ensure .env is loaded for DB credentials and GCP config
#         load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))

#         db_conn = DatabaseConnector() # Initialize DatabaseConnector
#         sql_agent = SQLAgent(db_conn) 

#         # Test 1: Simple query, one table
#         query1 = "What's the total estimated value of opportunities by their status?"
#         relevant_tables1 = [ "opportunities"] 
#         relevant_columns1 = [ "status", "estimated value"] 
#         print(f"\n--- Test 1: Query: '{query1}' ---")
#         sql1, schema_df1 = await sql_agent.generate_sql_query(query1, relevant_tables1, relevant_columns1)
#         print(f"\nGenerated SQL 1:\n{sql1}")
#         if not schema_df1.empty:
#             print("\nSchema used for SQL 1 (first 5 rows):\n", schema_df1.head().to_string())

#         # --- Execute the generated SQL query for Test 1 ---
#         if sql1 and not sql1.startswith("Error"):
#             print("\n--- Executing Generated SQL 1 ---")
#             try:
#                 query_results1 = await db_conn.execute_query(sql1) # Execute the query
#                 if query_results1:  # Check if list is not empty
#                     print("\nResults for SQL 1 (first 5 rows):")
#                     for row in query_results1[:5]:
#                         print(row)
#                     print(f"\nTotal rows returned for SQL 1: {len(query_results1)}")
#                 else:
#                     print("\nNo results found for SQL 1.")
#             except Exception as e:
#                 print(f"\nError executing SQL 1: {e}")
#                 logging.error(f"Error executing SQL 1: {e}", exc_info=True)

        # --- Test 2: Generate SQL and Execute ---
        # query2 = "Who is the customer for opportunity ID 1"  # Changed to specify ID for clarity
        # relevant_tables2 = ["opportunities", "sales_representatives", "customers"] 
        # relevant_columns2 = ["opportunity_name", "status", "name", "customer_name"] 
        # print(f"\n--- Test 2: Query: '{query2}' ---")
        # sql2, schema_df2 = await sql_agent.generate_sql_query(query2, relevant_tables2, relevant_columns2)
        # print(f"\nGenerated SQL 2:\n{sql2}")
        
        # if not schema_df2.empty:
        #     print("\nSchema used for SQL 2 (first 5 rows):\n", schema_df2.head().to_string())

        # # --- Execute the generated SQL query for Test 2 ---
        # if sql2 and not sql2.startswith("Error"):
        #     print("\n--- Executing Generated SQL 2 ---")
        #     try:
        #         query_results2 = await db_conn.execute_query(sql2) # Execute the query
        #         if query_results2:  # Check if list is not empty
        #             print("\nResults for SQL 2 (first 5 rows):")
        #             for row in query_results2[:5]:
        #                 print(row)
        #             print(f"\nTotal rows returned for SQL 2: {len(query_results2)}")
        #         else:
        #             print("\nNo results found for SQL 2.")
        #     except Exception as e:
        #         print(f"\nError executing SQL 2: {e}")
        #         logging.error(f"Error executing SQL 2: {e}", exc_info=True)

    # try:
    #     asyncio.run(run_sql_agent_tests())
    # except Exception as e:
    #     print(f"An error occurred during SQLAgent tests: {e}")