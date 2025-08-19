import os
import uvicorn
import aiohttp
import json
from fastapi import HTTPException, status
from pydantic import BaseModel, Field
from typing import Dict, Any, Optional, List
from dotenv import load_dotenv

# Import FastMCP and tool decorator
from mcp.server.fastmcp import FastMCP

# Load environment variables
load_dotenv()

# --- Configuration ---
CRM_API_BASE_URL = "http://localhost:5104"
CRM_MCP_SERVER_PORT = int(os.getenv("CRM_MCP_SERVER_PORT", 8001))

if not CRM_API_BASE_URL:
    raise ValueError("CRM_API_BASE_URL environment variable not set for CRM MCP Server. "
                     "Please set it to your FastAPI service URL, e.g., http://localhost:5104")

fastmcp = FastMCP(
    context="sales", # This server explicitly handles the 'sales' context
    title="CRM Model Context Protocol Server",
    description="Provides tools for managing CRM (Sales) leads, quotations, and opportunities.",
    version="1.0.0"
)

# --- Helper for Making Internal API Calls ---
async def _call_crm_api(method: str, url: str, json_data: Optional[Dict] = None) -> Any:
    """
    Generic helper to make asynchronous HTTP calls to the CRM API.
    """
    async with aiohttp.ClientSession() as session:
        try:
            if method.upper() == "GET":
                async with session.get(url, params=json_data) as response:
                    response.raise_for_status() # Raises an exception for 4xx/5xx responses
                    return await response.json()
            elif method.upper() == "PUT":
                async with session.put(url, json=json_data) as response:
                    response.raise_for_status()
                    return await response.json()
            elif method.upper() == "POST":
                async with session.post(url, json=json_data) as response:
                    response.raise_for_status()
                    return await response.json()
            else:
                raise ValueError(f"Unsupported HTTP method: {method}")
        except aiohttp.ClientResponseError as e:
            response_text = await e.response.text() if e.response else "N/A"
            print(f"[CRM MCP Server] Error calling CRM API: {method} {url} - Status: {e.status}, Message: {e.message}, Response: {response_text}")
            if e.status == 404:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Resource not found in CRM API at {url}. (Details: {response_text})")
            raise HTTPException(
                status_code=e.status,
                detail=f"CRM API Error: {e.message}. Context: {e.request_info.url}. Response: {response_text}"
            )
        except aiohttp.ClientConnectionError as e:
            print(f"[CRM MCP Server] Connection error to CRM API: {url} - {e}")
            raise HTTPException(
                status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
                detail=f"Could not connect to the CRM API at {url}. Is it running and accessible?"
            )
        except Exception as e:
            print(f"[CRM MCP Server] Unexpected error during CRM API call: {e}")
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"An unexpected error occurred: {str(e)}"
            )

# --- Input Model for the get_lead_info tool ---
class GetLeadInfoInput(BaseModel):
    id: int = Field(..., description="The unique integer primary key ID of the lead.")

# --- Implement the get_lead_info tool ---
@fastmcp.tool()
async def get_lead_info(input: GetLeadInfoInput) -> Dict[str, Any]:
    """
    Retrieves comprehensive details for a specific sales lead from the CRM system
    using its unique Lead key ID.
    Retrieves **general contact, status, and high-level details** for a specific sales lead.
    Use this tool when the user asks for basic information, contact details, or the overall status of a lead.
    Example questions: 'What are the details for lead 123?', 'Show me lead L001 info', 'Tell me about lead 5

    Parameters:
    - id (int): The unique integer primary key identifier of the lead.

    Returns:
    A dictionary containing the lead's comprehensive details in camelCase format.
    """
    lead_db_id = input.id
    api_url = f"{CRM_API_BASE_URL}/api/SalesLead/{lead_db_id}"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url} for DB ID {lead_db_id}")
    lead_data = await _call_crm_api("GET", api_url)
    return lead_data

# --- Input Model for get_sales_lead_quotations_with_items tool ---
class GetSalesLeadQuotationsWithItemsInput(BaseModel):
    leadId: str = Field(..., description="The human-readable identifier of the sales lead (e.g., 'LD00049').")

# --- Implement the get_sales_lead_quotations_with_items tool ---
@fastmcp.tool()
async def get_sales_lead_quotations_with_items(input: GetSalesLeadQuotationsWithItemsInput) -> List[Dict[str, Any]]:
    """
    Retrieves all sales quotations, including their associated product items, for a specific sales lead.
    Retrieves all **sales quotations and their associated product items** for a specific sales lead.
    Use this tool when the user specifically asks for quotes, quotations, pricing, or product details related to a lead.
    Example questions: 'What are the quotes for lead LD00049?', 'Show me quotations for lead L001 with items', 'Get the pricing for lead 5.'

    Parameters:
    - leadId (str): The human-readable identifier of the sales lead (e.g., 'LD00049').

    Returns:
    A list of dictionaries, where each dictionary represents a sales quotation
    and includes a nested list of its product items. All fields are in camelCase.
    """
    lead_human_id = input.leadId
    api_url = f"{CRM_API_BASE_URL}/api/SalesLead/{lead_human_id}/quotations-with-items"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url} for Lead ID {lead_human_id}")
    
    quotations_data = await _call_crm_api("GET", api_url)
    
    return quotations_data

# --- NEW TOOL: Get Sales Opportunity Card Counts ---
class GetSalesOpportunityCardCountsInput(BaseModel):
    pass # No input parameters needed as per the FastAPI endpoint

@fastmcp.tool()
async def get_sales_opportunity_card_counts(input: GetSalesOpportunityCardCountsInput) -> List[Dict[str, Any]]:
    """
    Retrieves the count of sales opportunities grouped by specific statuses for display on cards.
    This tool provides an aggregated count of active sales opportunities based on their current status
    (e.g., Identified, Solution Presentation, Proposal, Negotiation, Closed Won).
    Use this tool to get an overview of the sales pipeline by status or to populate dashboard cards.
    
    Example questions:
    - 'How many opportunities are in each status?'
    - 'Give me the counts for identified, proposal, and closed won opportunities.'
    - 'Show me the opportunity dashboard numbers.'
    
    Parameters:
    - This tool takes no parameters.

    Returns:
    A list of dictionaries, each containing 'status' (string) and 'count' (integer).
    Example:
    [
      {
        "status": "Identified",
        "count": 5
      },
      {
        "status": "Solution Presentation",
        "count": 3
      },
      {
        "status": "Proposal",
        "count": 7
      },
      {
        "status": "Negotiation",
        "count": 2
      },
      {
        "status": "Closed Won",
        "count": 10
      }
    ]
    """
    api_url = f"{CRM_API_BASE_URL}/api/SalesOpportunity/cards"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url}")
    
    card_counts_data = await _call_crm_api("GET", api_url)
    
    return card_counts_data

# --- Existing Tool: Get Active Sales Opportunities with Items ---
# No input parameters needed for this tool as per the SQL function
class GetActiveOpportunitiesWithItemsInput(BaseModel):
    pass # No fields, as the underlying function public.get_active_opportunities() takes no arguments

@fastmcp.tool()
async def get_active_opportunities_with_items(input: GetActiveOpportunitiesWithItemsInput) -> List[Dict[str, Any]]:
    """
    Retrieves all active sales opportunities, including their associated product items.
    This tool provides a comprehensive list of all sales opportunities that are currently active,
    along with detailed information about the products associated with each opportunity.
    Use this tool when the user asks for a list of active opportunities,
    wants to see all opportunities with their products, or asks about current sales pipeline items.
    
    Example questions:
    - 'Show me all active sales opportunities.'
    - 'What are the current opportunities in the sales pipeline?'
    - 'List all opportunities along with the products involved.'
    
    Parameters:
    - This tool takes no parameters.

    Returns:
    A list of dictionaries, where each dictionary represents a sales opportunity
    and includes a nested list of its product items. All fields are in camelCase.
    """
    api_url = f"{CRM_API_BASE_URL}/api/SalesOpportunity/with-items"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url}")
    
    opportunities_data = await _call_crm_api("GET", api_url)
    
    return opportunities_data


# --- NEW TOOL (PLACEHOLDER): Get Single Opportunity by ID with Items ---
# IMPORTANT: This API endpoint (`/api/SalesOpportunity/with-items/{idOrOpportunityId}`)
# DOES NOT EXIST IN YOUR FastAPI backend (`main.py`) YET.
# You will need to implement it in main.py for this tool to function correctly.
class GetOpportunityByIdWithItemsInput(BaseModel):
    id_or_opportunity_id: str = Field(..., description="The unique integer primary key ID (e.g., '123') or the human-readable Opportunity ID (e.g., 'OP001') of the sales opportunity.")

@fastmcp.tool()
async def get_opportunity_by_id_with_items(input: GetOpportunityByIdWithItemsInput) -> Optional[Dict[str, Any]]:
    """
    if they need details with items then only this need to use 
    Retrieves a specific sales opportunity along with its associated product items by its ID.
    This tool is used to fetch detailed information for a single sales opportunity,
    including the products that are part of it, using either its internal primary key ID or its human-readable Opportunity ID.
    
    Use this tool when the user asks for details about a specific opportunity,
    e.g., 'Tell me about opportunity OP005 with its items', 'What are the products for opportunity ID 12?',
    'Get me the details for the "New Client Onboarding" opportunity.'
    
    Parameters:
    - id_or_opportunity_id (str): The unique identifier of the sales opportunity. This can be either
                                  the integer primary key ID (e.g., '123') or the human-readable
                                  Opportunity ID (e.g., 'OP001'). The tool will attempt to resolve
                                  which type of ID it is.
                                  
    Returns:
    A dictionary containing the opportunity's comprehensive details and its product items in camelCase format,
    or None if the opportunity is not found.
    """
    # Note: Your FastAPI backend (main.py) currently does NOT have an endpoint
    # like /api/SalesOpportunity/with-items/{id}.
    # You would need to add an endpoint to main.py that handles fetching a single
    # opportunity by ID (either integer PK or string Opportunity ID) and returns its items.
    # For example:
    # @app.get("/api/SalesOpportunity/with-items/{identifier}", response_model=SalesOpportunityRead)
    # async def get_single_opportunity_with_items(identifier: str, conn: asyncpg.Connection = Depends(get_db_connection)):
    #     # Add logic here to query sales_opportunities by either integer ID or opportunity_id string
    #     # and return the SalesOpportunityRead model.
    
    api_url = f"{CRM_API_BASE_URL}/api/SalesOpportunity/with-items/{input.id_or_opportunity_id}"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url} for Opportunity ID {input.id_or_opportunity_id}")

    try:
        opportunity_data = await _call_crm_api("GET", api_url)
        return opportunity_data
    except HTTPException as e:
        if e.status_code == status.HTTP_404_NOT_FOUND:
            print(f"[CRM MCP Server Tool] Opportunity with ID or Opportunity ID '{input.id_or_opportunity_id}' not found.")
            return None
        raise # Re-raise other HTTP exceptions

@fastmcp.tool()

class GetSalesOpportunityInput(BaseModel):
    """Input for retrieving a sales opportunity by its ID."""
    opportunityId: str = Field(
        ...,
        description="The unique string identifier of the sales opportunity (e.g., 'OPP00001')."
    )

async def get_sales_opportunity_by_id(input: GetSalesOpportunityInput) -> Dict[str, Any]:
    """
    Retrieves comprehensive details for a specific sales opportunity from the CRM system
    using its unique string opportunity ID.
    
    Retrieves **general information, status, and high-level details** for a specific sales opportunity.
    Use this tool when the user asks for information about a sales opportunity by its ID.
    
    Example questions: 'What are the details for sales opportunity OPP00001?', 'Show me opportunity 5678', 'Tell me about the sales opp 901'
    
    Parameters:
    - opportunityId (str): The unique string identifier of the sales opportunity.
    
    Returns:
    A dictionary containing the sales opportunity's comprehensive details in camelCase format.
    """
    opportunity_id = input.opportunityId
    # Assuming CRM_API_BASE_URL is a global constant
    api_url = f"{CRM_API_BASE_URL}/api/SalesOpportunity/{opportunity_id}"
    print(f"[CRM MCP Server Tool] Calling CRM API (GET): {api_url} for Opportunity ID {opportunity_id}")
    
    try:
        result = await _call_crm_api("GET", api_url)
        return result
    except Exception as e:
        print(f"Error calling CRM API for opportunity {opportunity_id}: {e}")
        return {"error": str(e)}



if __name__ == "__main__":
    print(f"[CRM MCP Server] Starting CRM MCP Server on http://localhost:5104")
    print(f"[CRM MCP Server] MCP Context: 'sales'")
    print(f"[CRM MCP Server] Exposed Tools: get_lead_info, get_sales_lead_quotations_with_items, get_sales_opportunity_card_counts, get_active_opportunities_with_items, get_opportunity_by_id_with_items")

    # This runs the FastMCP application
    uvicorn.run(fastmcp.streamable_http_app, host="localhost", port=CRM_MCP_SERVER_PORT)