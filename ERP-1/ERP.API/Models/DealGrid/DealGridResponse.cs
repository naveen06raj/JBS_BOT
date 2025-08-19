using System;
using System.Collections.Generic;

namespace ERP.API.Models.DealGrid
{
    public class DealGridResponse
    {
        public int Id { get; set; }
        public string? DealName { get; set; }
        public decimal Amount { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public string? Status { get; set; }
        public string? DealFor { get; set; }
        public string? Comments { get; set; }
        public string? CustomerName { get; set; }
        public string? OpportunityName { get; set; }
        public string? SalesRepresentative { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }

    public class DealGridPaginatedResponse
    {
        public List<DealGridResponse> Deals { get; set; } = new();
        public int TotalRecords { get; set; }
    }
}
