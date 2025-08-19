# Sales Lead Dashboard API

This document describes the new API endpoints for the Sales Lead Dashboard functionality.



## Lead Filtering

Filter leads based on various criteria:
- Territory
- Customer Name
- Status
- Score
- Lead Type

Also supports pagination, sorting, and counting.

### Endpoint

```
POST: /api/SalesLeadDashboard/filter
```

### Request Example

```json
{
  "territory": "North",
  "customerName": "Hospital",
  "status": "New",
  "score": "High",
  "leadType": "Medical",
  "sortField": "date_created",
  "sortDirection": "DESC",
  "pageNumber": 1,
  "pageSize": 10
}
```

### Response Example

```json
{
  "leads": [
    {
      "id": 53,
      "customerName": "ABC Hospital",
      "territoryName": "North",
      "status": "New",
      "score": 85,
      "leadType": "Medical",
      "createdDate": "2025-05-28T10:20:30",
      "contactName": "John Doe",
      "contactEmail": "john@example.com",
      "priority": "High",
      "totalCount": 124
    },
    ...
  ],
  "totalCount": 124
}
```

## My Leads

Get leads assigned to a specific user.

### Endpoint

```
POST: /api/SalesLeadDashboard/my-leads/{userId}
```

### Request Example

```json
{
  "sortField": "date_created",
  "sortDirection": "DESC",
  "pageNumber": 1,
  "pageSize": 10
}
```

### Response Example

Same as the filter response.

## Dropdown Options

Get all available filter options for dropdowns.

### Endpoint

```
GET: /api/SalesLeadDashboard/dropdown-options
```

### Response Example

```json
{
  "territories": ["North", "South", "East", "West"],
  "customers": ["ABC Hospital", "XYZ Clinic", ...],
  "statuses": ["New", "Contacted", "Qualified", "Converted", "Lost"],
  "scores": ["Low", "Medium", "High"],
  "leadTypes": ["Medical", "Pharmaceutical", "Equipment", ...]
}
```

## Database Functions

The API is backed by the following PostgreSQL functions:

1. `sp_sales_lead_summary_cards()` - Returns summary card data
2. `sp_sales_lead_filter(...)` - Filters leads with various criteria
3. `sp_sales_lead_my_leads(...)` - Returns leads assigned to a specific user
4. `sp_sales_lead_dropdown_options()` - Returns options for filter dropdowns

## Architecture

1. **SQL Functions** - Located in `Sqlscript/SalesLeadSummary.sql`
2. **Models** - See `SalesLeadFilterRequest.cs`, `SalesLeadFilterResult.cs`
3. **Services** - Methods implemented in `SalesLeadApiService.cs`
4. **Controller** - `SalesLeadDashboardController.cs`
