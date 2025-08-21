# src/utils/schema_updater.py

import os
import pandas as pd
import ast # For safely evaluating the literal dictionary from the file
import json
import logging
import importlib
import asyncio
import pprint # For robust Python literal output
from typing import Dict, List, Any

from database.db_connector import DatabaseConnector
from database.Schema_full import GET_SCHEMA_SQL, fetch_full_schema_dataframe # To get the schema DataFrame

# Set up basic logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# Path to the Schema_map.py file that will be read and rewritten
SCHEMA_MAP_PY_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), '../database/Schema_map.py')

async def update_schema_map_file(db_connector: DatabaseConnector):
    """
    Connects to the database, fetches the full schema, and then updates
    and rewrites the Schema_map.py file, preserving custom semantic data.
    The output format of columns will be simple lists of column names.
    """
    logging.info("Starting schema map update (focus on DB sync and preserving manual data)...")
    
    try:
        # 1. Load the existing SCHEMA_MAP from Schema_map.py
        current_schema_map: Dict[str, Any] = {}
        if os.path.exists(SCHEMA_MAP_PY_PATH):
            with open(SCHEMA_MAP_PY_PATH, 'r') as f:
                content = f.read()
                # Find the SCHEMA_MAP dictionary definition
                schema_map_start = content.find("SCHEMA_MAP = {")
                if schema_map_start != -1:
                    # Extract the dictionary string
                    # This relies on the format being consistent
                    schema_map_str = content[schema_map_start + len("SCHEMA_MAP = "):].strip()
                    try:
                        current_schema_map = ast.literal_eval(schema_map_str)
                    except ValueError as ve:
                        logging.error(f"Error parsing existing SCHEMA_MAP.py content: {ve}. Please ensure it's a valid Python dictionary literal. Starting with empty map.")
                        current_schema_map = {}
                else:
                    logging.warning(f"Could not find 'SCHEMA_MAP = {{' in {SCHEMA_MAP_PY_PATH}. Starting with empty base.")
        else:
            logging.warning(f"{SCHEMA_MAP_PY_PATH} not found. Creating a new one from scratch if DB has tables.")

        # 2. Fetch the full, current schema from the database
        full_schema_df = await fetch_full_schema_dataframe(db_connector)

        if full_schema_df.empty:
            logging.warning("Fetched empty schema from database. Schema_map.py will reflect current (potentially empty) database state.")
        
        # 3. Create a live database table-to-columns mapping for easy lookup
        live_db_tables_columns: Dict[str, List[str]] = {}
        if not full_schema_df.empty:
            for table_name in full_schema_df['TABLE_NAME'].unique():
                # We only need the column names, no PK/FK/data type info for this output format
                columns_in_table = full_schema_df[full_schema_df['TABLE_NAME'] == table_name]['COLUMN_NAME'].tolist()
                live_db_tables_columns[table_name] = columns_in_table
        
        # 4. Create the new SCHEMA_MAP by merging current and live data
        updated_schema_map = {}
        processed_db_tables = set() # To track tables from live DB that have been covered by current_schema_map

        # First, iterate through the existing SCHEMA_MAP entries
        for key, value in current_schema_map.items():
            # Check if this entry is a table mapping (has a 'table' key which is a string)
            if "table" in value and isinstance(value["table"], str):
                table_name_in_db = value["table"]
                
                # Check if this database table still exists in the live database
                if table_name_in_db in live_db_tables_columns:
                    # Preserve all existing semantic data (synonyms, status_options, etc.)
                    updated_entry = value.copy()
                    # Update 'columns' with the live list from the database (simple names)
                    updated_entry["columns"] = live_db_tables_columns[table_name_in_db]
                    updated_schema_map[key] = updated_entry
                    processed_db_tables.add(table_name_in_db)
                else:
                    logging.warning(f"Table '{table_name_in_db}' (from conceptual key '{key}') not found in live database. Removing this entry.")
                    # If the underlying DB table is gone, we remove its conceptual mapping.
            else:
                # This handles 'Special Cases' like 'revenue' and 'location'
                # which don't have a simple "table" key. Preserve them as-is.
                updated_schema_map[key] = value

        # 5. Add any new tables found in the live database that are not yet in our conceptual map
        for live_table_name, live_columns in live_db_tables_columns.items():
            if live_table_name not in processed_db_tables:
                # Check if this live table is already covered by an existing entry's 'table' value
                # (e.g., if 'customers' is covered by 'customer' conceptual entry)
                is_covered_by_existing_conceptual_key = False
                for existing_key, existing_value in current_schema_map.items():
                    if "table" in existing_value and existing_value["table"] == live_table_name:
                        is_covered_by_existing_conceptual_key = True
                        break
                
                if not is_covered_by_existing_conceptual_key:
                    logging.info(f"New table '{live_table_name}' found in database. Adding a basic entry.")
                    # Add a new entry to the updated map, using the table name as the conceptual key
                    updated_schema_map[live_table_name] = {
                        "table": live_table_name,
                        "columns": live_columns,
                        "synonyms": [] # Default empty synonyms for new tables
                    }

        # 6. Rewrite the Schema_map.py file
        # Use pprint for robust Python literal output
        pp = pprint.PrettyPrinter(indent=4, width=120)
        literal_str = pp.pformat(updated_schema_map)

        # Ensure the directory exists before writing the file
        os.makedirs(os.path.dirname(SCHEMA_MAP_PY_PATH), exist_ok=True)

        with open(SCHEMA_MAP_PY_PATH, 'w') as f:
            f.write("# This file is automatically generated and updated by schema_updater.py\n")
            f.write("# DO NOT EDIT MANUALLY - your changes will be overwritten.\n\n")
            f.write("SCHEMA_MAP = \\\n")
            f.write(literal_str) 
            f.write("\n")
        
        logging.info("Schema_map.py updated successfully with live database structure and preserved manual data.")
        return True

    except Exception as e:
        logging.error(f"Error updating schema map: {e}", exc_info=True)
        return False

# Function to dynamically reload the Schema_map module (if needed by main app)
def reload_schema_map_module():
    """
    Forces a reload of the Schema_map module in memory.
    This is used by main.py to get the freshest SCHEMA_MAP after an update.
    """
    try:
        module_name = 'src.database.Schema_map'
        if module_name in importlib.sys.modules:
            module = importlib.reload(importlib.sys.modules[module_name])
        else:
            module = importlib.import_module(module_name)
        
        logging.info(f"{module_name} module reloaded successfully.")
        return module.SCHEMA_MAP
    except Exception as e:
        logging.error(f"Error reloading {module_name} module: {e}")
        return {} # Return empty on failure

# --- Test block for schema_updater.py ---
if __name__ == "__main__":
    async def run_schema_updater_tests():
        print("--- Running schema_updater.py tests ---")
        
        # Ensure .env is loaded for db_connector
        from dotenv import load_dotenv
        # Adjust path as needed if you run this from a different directory
        load_dotenv(dotenv_path=os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../config/.env'))
        
        # Create an instance of DatabaseConnector
        db_conn = DatabaseConnector()

        print("\nAttempting to update schema map file:")
        await update_schema_map_file(db_conn)
        
        print("\n--- schema_updater.py tests finished ---")

    asyncio.run(run_schema_updater_tests())