import pandas as pd
from typing import List
import logging

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

async def get_refined_schema_for_llm(
    full_schema_df: pd.DataFrame, 
    relevant_tables: List[str], 
    relevant_columns: List[str]
) -> pd.DataFrame:
    if full_schema_df.empty:
        logging.warning("Schema Comparer: Full schema DataFrame is empty, cannot refine hints.")
        return pd.DataFrame()

    logging.debug(f"Schema Comparer: Input relevant_tables: {relevant_tables}")
    logging.debug(f"Schema Comparer: Input relevant_columns: {relevant_columns}")

    relevant_tables_upper = [t.upper() for t in relevant_tables]
    relevant_columns_upper = [c.upper() for c in relevant_columns]

    logging.debug(f"Schema Comparer: Uppercased relevant_tables: {relevant_tables_upper}")
    logging.debug(f"Schema Comparer: Uppercased relevant_columns: {relevant_columns_upper}")
    
    # --- CRITICAL INSPECTION POINT 1 ---
    # What are the actual table names from the database?
    db_table_names = full_schema_df['TABLE_NAME'].unique().tolist()
    logging.debug(f"Schema Comparer: Actual table names from full_schema_df: {db_table_names}")


    # 1. Start with all columns from the initially relevant tables
    refined_schema_df = full_schema_df[
        full_schema_df['TABLE_NAME'].isin(relevant_tables_upper)
    ].copy() 

    # --- CRITICAL INSPECTION POINT 2 ---
    # Check if this initial filtering yielded any results
    logging.debug(f"Schema Comparer: refined_schema_df after initial filter (rows): {len(refined_schema_df)}")
    if refined_schema_df.empty:
        logging.warning(f"Schema Comparer: Initial filter (TABLE_NAME.isin(relevant_tables_upper)) resulted in an empty DataFrame.")
        # This is likely the exact point of failure.
        # Check if any of relevant_tables_upper are in db_table_names (from above debug print)
        missing_tables = [t for t in relevant_tables_upper if t not in db_table_names]
        if missing_tables:
            logging.warning(f"Schema Comparer: Tables from Router not found in DB schema: {missing_tables}")
        # If execution reaches here, it means no intersection, so just return empty.
        return pd.DataFrame() # Return early if nothing found at this initial stage


    current_tables_in_scope = set(refined_schema_df['TABLE_NAME'].unique())
    logging.debug(f"Schema Comparer: Tables currently in scope: {list(current_tables_in_scope)}")

    # 2. Iteratively add tables and their necessary PKs/FKs if they are referenced by FKs
    tables_referenced_by_fks_in_scope = set(
        refined_schema_df[refined_schema_df['COLUMN_KEY'] == 'MUL']['REFERENCED_TABLE_NAME'].dropna().unique()
    )
    new_tables_to_add = tables_referenced_by_fks_in_scope - current_tables_in_scope

    if new_tables_to_add:
        logging.info(f"Schema Comparer: Adding tables due to FK relationships: {list(new_tables_to_add)}")
        newly_added_df = full_schema_df[
            full_schema_df['TABLE_NAME'].isin(list(new_tables_to_add))
        ]
        refined_schema_df = pd.concat([refined_schema_df, newly_added_df]).drop_duplicates().reset_index(drop=True)
        current_tables_in_scope.update(new_tables_to_add)

    # 3. Ensure all Primary Key (PRI) columns for *all* tables now in scope are included.
    pk_cols_to_add_df = full_schema_df[
        (full_schema_df['TABLE_NAME'].isin(current_tables_in_scope)) &
        (full_schema_df['COLUMN_KEY'] == 'PRI')
    ]
    if not pk_cols_to_add_df.empty:
        refined_schema_df = pd.concat([refined_schema_df, pk_cols_to_add_df]).drop_duplicates().reset_index(drop=True)

    # 4. Final selection of columns for the LLM:
    final_schema_df = refined_schema_df

    # Sort for consistent output
    final_schema_df = final_schema_df.sort_values(by=['TABLE_NAME', 'COLUMN_NAME']).reset_index(drop=True)
    
    if final_schema_df.empty and (relevant_tables or relevant_columns):
        logging.warning(f"Schema Comparer: No schema information found after refinement for tables {relevant_tables} and columns {relevant_columns}.")
    elif not final_schema_df.empty:
        logging.info(f"Schema Comparer: Refined schema contains {len(final_schema_df['TABLE_NAME'].unique())} tables and {len(final_schema_df)} columns.")
    
    return final_schema_df