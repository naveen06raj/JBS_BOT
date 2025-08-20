# src/database/db_connector.py

import os
from dotenv import load_dotenv
import asyncpg
from fastapi import HTTPException, status # Keep if you're using FastAPI, otherwise can remove
import asyncio 
from typing import Dict, Any, List # Added for type hinting

# Load environment variables from .env file.
# Adjust the path if your .env file is located elsewhere.
# This path assumes .env is in a 'config' folder one level up from 'src/database'
load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))

# --- Database Configuration ---
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "DOT_JBS") 
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "Mvlabs") 

DATABASE_URL = (
    f"postgresql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{DB_NAME}"
)

# --- Asynchronous Database Connection Dependency (for FastAPI/similar) ---
async def get_db_connection():
    """
    Dependency to provide an asyncpg database connection.
    Yields a connection which is automatically closed after use.
    This function is typically used by asynchronous web frameworks (e.g., FastAPI).
    """
    conn = None
    try:
        conn = await asyncpg.connect(DATABASE_URL)
        print("Database connection established successfully (async).")
        yield conn
    except Exception as e:
        print(f"Database connection error: {e}")
        # Re-raise as HTTPException for API context, or a custom exception for CLI
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Could not connect to the database.")
    finally:
        if conn:
            await conn.close()
            print("Database connection closed (async).")

# --- Generic Asynchronous Query Execution ---
async def execute_query_async(conn: asyncpg.Connection, query: str, params: tuple = None, fetch: bool = True):
    """
    Executes an asynchronous SQL query using a given asyncpg connection.

    Args:
        conn (asyncpg.Connection): The active asyncpg database connection.
        query (str): The SQL query string.
        params (tuple, optional): Parameters to pass to the query (for parameterized queries).
                                  Defaults to None.
        fetch (bool, optional): Whether to fetch results (for SELECT queries). Defaults to True.

    Returns:
        list[dict]: List of fetched rows (as dictionaries) if fetch is True, otherwise None.
    """
    try:
        if fetch:
            rows = await conn.fetch(query, *(params if params is not None else ()))
            return [dict(r) for r in rows]
        else:
            await conn.execute(query, *(params if params is not None else ()))
            return None
    except Exception as e:
        print(f"Error executing asynchronous query: {e}")
        print(f"Query: {query}")
        raise e # Re-raise for proper error handling

# --- Main DatabaseConnector Class (Optional, but useful for structured access) ---
class DatabaseConnector:
    def __init__(self):
        self.database_url = DATABASE_URL

    async def get_connection(self) -> asyncpg.Connection:
        """Establishes and returns a new asyncpg connection."""
        return await asyncpg.connect(self.database_url)

    async def execute_query(self, query: str, params: tuple = None, fetch: bool = True) -> List[Dict[str, Any]]:
        """
        Executes an SQL query and manages connection lifecycle.
        Returns a list of dictionaries for fetched results.
        """
        conn = None
        try:
            conn = await self.get_connection()
            return await execute_query_async(conn, query, params, fetch)
        except Exception as e:
            print(f"Error in DatabaseConnector.execute_query: {e}")
            raise # Re-raise the exception after printing
        finally:
            if conn:
                await conn.close()

# --- Test block for db_connector.py (OPTIONAL, but good for testing this module) ---
if __name__ == "__main__":
    async def run_db_connector_tests():
        print("--- Running db_connector.py tests ---")
        db_conn = DatabaseConnector()
        
        print(f"DEBUG: Using DATABASE_URL={db_conn.database_url}")
        print("\nAttempting a simple SELECT query execution:")
        try:
            # Test with a very simple, known query, as GET_SCHEMA_SQL is no longer here
            test_query = "SELECT 1 as test_column, 'hello' as message;" 
            test_results = await db_conn.execute_query(test_query)
            if test_results:
                print(f"Test query successful. Results: {test_results}")
            else:
                print("Test query returned no results.")
        except Exception as e:
            print(f"An error occurred during general query test: {e}")

        print("\n--- db_connector.py tests finished ---")

    asyncio.run(run_db_connector_tests())