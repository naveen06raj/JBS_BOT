using System;
using System.Text.Json.Serialization;

namespace ERP.API.Models
{
    public class QuotationGridResult
    {
        public int TotalRecords { get; set; }
        public int Id { get; set; }
        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string? Version { get; set; }
        public string? Terms { get; set; }
        public DateTime? ValidTill { get; set; }
        public string? QuotationFor { get; set; }
        public string? Status { get; set; }
        public string? LostReason { get; set; }
        public int? CustomerId { get; set; }
        public string? QuotationType { get; set; }
        public DateTime? QuotationDate { get; set; }
        public string? OrderType { get; set; }
        public string? Comments { get; set; }
        public string? DeliveryWithin { get; set; }
        public string? DeliveryAfter { get; set; }
        public bool IsActive { get; set; }
        public string? QuotationId { get; set; }
        public int? OpportunityId { get; set; }
        public int? LeadId { get; set; }
        public string? CustomerName { get; set; }
        public string? Taxes { get; set; }
        public string? Delivery { get; set; }
        public string? Payment { get; set; }
        public string? Warranty { get; set; }
        public string? FreightCharge { get; set; }
        public bool? IsCurrent { get; set; }
        public int? ParentSalesQuotationsId { get; set; }
        public object? Products { get; set; }
    }
}
