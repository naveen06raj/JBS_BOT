# src/database/Schema_full.py

import pandas as pd
from typing import Dict, Any, List
import asyncpg
import asyncio
import os
# from dotenv import load_dotenv # Removed as per user preference

# Corrected Import: Only DatabaseConnector is directly used here
from src.database.db_connector import DatabaseConnector

# --- SQL Query to Fetch Full Schema Details (FINAL COMPREHENSIVE VERSION FOR YOUR DB) ---
# This query correctly handles your information_schema structure to get FK details.
GET_SCHEMA_SQL = """
SELECT
    c.table_name,
    c.column_name,
    c.data_type,
    CASE
        WHEN pk.column_name IS NOT NULL THEN 'PRI' -- Primary Key
        WHEN fk_local.column_name IS NOT NULL THEN 'MUL' -- Multiple Key (indicates a Foreign Key)
        ELSE NULL
    END AS column_key,
    rc.unique_constraint_schema AS referenced_schema_name,
    kcu_referenced.table_name AS referenced_table_name,
    kcu_referenced.column_name AS referenced_column_name
FROM
    information_schema.columns AS c
LEFT JOIN
    information_schema.table_constraints AS tc_pk
    ON tc_pk.table_schema = c.table_schema 
    AND tc_pk.table_name = c.table_name 
    AND tc_pk.constraint_type = 'PRIMARY KEY'
LEFT JOIN
    information_schema.key_column_usage AS pk
    ON pk.constraint_name = tc_pk.constraint_name 
    AND pk.table_schema = c.table_schema 
    AND pk.table_name = c.table_name 
    AND pk.column_name = c.column_name
LEFT JOIN
    -- This identifies the local columns that are part of a foreign key constraint
    information_schema.key_column_usage AS fk_local
    ON fk_local.table_schema = c.table_schema
    AND fk_local.table_name = c.table_name
    AND fk_local.column_name = c.column_name
LEFT JOIN
    -- This links the local foreign key constraint (fk_local) to its referential constraint
    information_schema.referential_constraints AS rc
    ON rc.constraint_schema = fk_local.constraint_schema
    AND rc.constraint_name = fk_local.constraint_name
LEFT JOIN
    -- This finds the actual primary key column(s) in the referenced table using the unique constraint info from rc
    information_schema.key_column_usage AS kcu_referenced
    ON kcu_referenced.constraint_schema = rc.unique_constraint_schema
    AND kcu_referenced.constraint_name = rc.unique_constraint_name
    AND kcu_referenced.ordinal_position = fk_local.position_in_unique_constraint -- Crucial for composite keys, links FK column to PK column position
WHERE
    c.table_schema = 'public' -- Specify 'public' schema or your custom schema name
ORDER BY
    c.table_name, c.ordinal_position;
"""

async def fetch_full_schema_dataframe(db_connector: DatabaseConnector) -> pd.DataFrame:
    """
    Fetches the entire database schema using DatabaseConnector and returns it as a Pandas DataFrame.
    This DataFrame is suitable for use by SQLAgent and SchemaUpdater.
    """
    print("Attempting to fetch full database schema for DataFrame creation (async).")
    try:
        # Use the DatabaseConnector instance to execute the query
        rows = await db_connector.execute_query(GET_SCHEMA_SQL, fetch=True)
        
        if rows:
            df = pd.DataFrame(rows)
            # Ensure consistent column names (lowercase to uppercase for consistency with previous examples)
            df.columns = [col.upper() for col in df.columns] 
            # Rename specific columns for clarity with previous logic if needed (adjust based on your GET_SCHEMA_SQL output)
            df.rename(columns={
                'TABLE_NAME': 'TABLE_NAME',
                'COLUMN_NAME': 'COLUMN_NAME',
                'DATA_TYPE': 'DATA_TYPE',
                'COLUMN_KEY': 'COLUMN_KEY', # 'PRI', 'MUL', or None
                'REFERENCED_SCHEMA_NAME': 'REFERENCED_SCHEMA_NAME', # Added this for completeness
                'REFERENCED_TABLE_NAME': 'REFERENCED_TABLE_NAME',
                'REFERENCED_COLUMN_NAME': 'REFERENCED_COLUMN_NAME'
            }, inplace=True)
            
            # --- ADD THESE LINES TO NORMALIZE CASE ---
            df['TABLE_NAME'] = df['TABLE_NAME'].str.upper()
            df['COLUMN_NAME'] = df['COLUMN_NAME'].str.upper()
            if 'REFERENCED_TABLE_NAME' in df.columns:
                df['REFERENCED_TABLE_NAME'] = df['REFERENCED_TABLE_NAME'].str.upper()
            if 'REFERENCED_COLUMN_NAME' in df.columns:
                df['REFERENCED_COLUMN_NAME'] = df['REFERENCED_COLUMN_NAME'].str.upper()
            # --- END ADDED LINES ---

            # Fill NaN for referenced tables/columns where no FK exists with empty strings
            df['REFERENCED_SCHEMA_NAME'] = df['REFERENCED_SCHEMA_NAME'].fillna('')
            df['REFERENCED_TABLE_NAME'] = df['REFERENCED_TABLE_NAME'].fillna('')
            df['REFERENCED_COLUMN_NAME'] = df['REFERENCED_COLUMN_NAME'].fillna('')
            df['COLUMN_KEY'] = df['COLUMN_KEY'].fillna('') # Fill None with empty string for cleaner checks in validation
            
            print("Database schema fetched and converted to DataFrame successfully.")
            return df
        else:
            print("No schema rows fetched. This might mean no tables in the specified schema or a filter issue.")
            return pd.DataFrame() # Return empty DataFrame
    except Exception as e:
        print(f"Error fetching full database schema for DataFrame: {e}")
        print(f"This error often indicates a problem with: ")
        print(f" 1. Database server not running or inaccessible.")
        print(f" 2. Incorrect database credentials (user/password) in src/database/db_connector.py.")
        print(f" 3. Network/firewall issues.")
        print(f" 4. The SQL query itself (GET_SCHEMA_SQL) or the database's information_schema structure.")
        return pd.DataFrame() # Return empty DataFrame on error

# --- Test block to verify schema fetching directly in this file ---
if __name__ == "__main__":
    async def run_schema_full_tests():
        print("--- Running Schema_full.py tests ---")
        
        # Create an instance of DatabaseConnector
        db_conn = DatabaseConnector() 

        # Debugging: Print the DATABASE_URL that will be used (from db_connector)
        print(f"DEBUG: Using DATABASE_URL={db_conn.database_url}")
        
        # Test schema fetching
        print("\nAttempting to fetch database schema as DataFrame:")
        schema_df = await fetch_full_schema_dataframe(db_conn) # Pass the instance to the function
        
        if not schema_df.empty:
            print(f"Schema DataFrame fetched successfully. Found {len(schema_df['TABLE_NAME'].unique())} tables.")
            print("\n--- Full Fetched Schema DataFrame (first 5 rows) ---")
            print(schema_df.head().to_string()) # Print head of the DataFrame
            
            # Example: Try to print all columns for 'customers' table if it exists
            # This section is modified to show ALL details for 'customers' or all unique tables
            # IMPORTANT: Now check for 'CUSTOMERS' in uppercase due to the normalization
            if 'DEMOS' in schema_df['TABLE_NAME'].unique(): # Use uppercase for consistency
                print("\n--- Example: All columns for 'DEMOS' table ---")
                print(schema_df[schema_df['TABLE_NAME'] == 'DEMOS'].to_string())
            else:
                print("\n--- Note: 'DEMOS' table not found in schema. Showing all unique table names instead. ---")
                print(schema_df['TABLE_NAME'].unique())

            print("\n--- End Full Fetched Schema ---")
        else:
            print("Failed to fetch schema DataFrame. Please check the debug messages above and your DB content/schema.")
        
        print("\n--- Schema_full.py tests finished ---")

    asyncio.run(run_schema_full_tests())