using System;

namespace ERP.API.Models
{
    public class SalesTermsAndConditions
    {
        public int Id { get; set; }
        public int UserCreated { get; set; }
        public DateTime DateCreated { get; set; }
        public int UserUpdated { get; set; }
        public DateTime DateUpdated { get; set; }        public string? Taxes { get; set; }
        public string? FreightCharges { get; set; }
        public string? Delivery { get; set; }
        public string? Payment { get; set; }
        public string? Warranty { get; set; }
        public string? TemplateName { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int? QuotationId { get; set; }
    }
}
