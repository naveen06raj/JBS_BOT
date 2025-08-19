using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ERP.API.Models.DTOs
{    public class CreateQuotationRequestDto
    {
        public int? UserCreated { get; set; } = 1;
        public int? UserUpdated { get; set; }
        [StringLength(255)]
        public string? Version { get; set; }
        
        [StringLength(255)]
        public string? Terms { get; set; }
        
        public DateTime? ValidTill { get; set; }
        
        [StringLength(255)]
        public string? QuotationFor { get; set; }
        
        [StringLength(255)]
        public string Status { get; set; } = "Draft";
          [StringLength(255)]
        public string? LostReason { get; set; }
          public int? CustomerId { get; set; }
        
        [StringLength(255)]
        public string QuotationType { get; set; } = string.Empty;
          public DateTime? QuotationDate { get; set; }
        
        [StringLength(255)]
        public string? OrderType { get; set; }
        
        [StringLength(255)]
        public string? Comments { get; set; }
        
        [StringLength(255)]
        public string? DeliveryWithin { get; set; }
        
        [StringLength(255)]
        public string? DeliveryAfter { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(255)]
        public string? QuotationId { get; set; }
        
        public int? OpportunityId { get; set; }
        
        public int? LeadId { get; set; }
          [StringLength(255)]
        public string? CustomerName { get; set; }
        
        [StringLength(255)]
        public string? Taxes { get; set; }
        
        [StringLength(255)]
        public string? Delivery { get; set; }
        
        [StringLength(255)]
        public string? Payment { get; set; }
        
        [StringLength(255)]
        public string? Warranty { get; set; }
        
        [StringLength(255)]
        public string? FreightCharge { get; set; }
          public bool IsCurrent { get; set; } = true;
        
        public int? ParentSalesQuotationsId { get; set; }
    }

    public class UpdateQuotationRequestDto : CreateQuotationRequestDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }
    }

    public class QuotationResponseDto
    {
        // Base audit fields
        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }

        // Quotation fields
        public int Id { get; set; }
        public string? QuotationType { get; set; }
        public string? OrderType { get; set; }
        public DateTime? QuotationDate { get; set; }
        public string? Status { get; set; }
        public string? Version { get; set; }
        public string? Terms { get; set; }
        public DateTime? ValidTill { get; set; }
        public string? QuotationFor { get; set; }
        public string? LostReason { get; set; }
        public int? CustomerId { get; set; }
        public string? Comments { get; set; }
        public string? DeliveryWithin { get; set; }
        public string? DeliveryAfter { get; set; }
        public bool IsActive { get; set; }
        public string? QuotationId { get; set; }
        public int? OpportunityId { get; set; }
        public int? LeadId { get; set; }
        public string? Taxes { get; set; }
        public string? Delivery { get; set; }
        public string? Payment { get; set; }
        public string? Warranty { get; set; }
        public string? FreightCharge { get; set; }
        public bool? IsCurrent { get; set; }
        public int? ParentSalesQuotationsId { get; set; }

        // Sales Order fields
        public int? SalesOrderId { get; set; }
        public string? OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? OrderStatus { get; set; }
        public string? PoId { get; set; }
        public DateTime? AcceptanceDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrandTotal { get; set; }
        public string? Notes { get; set; }

        // Customer and Contact Information
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerMobile { get; set; }
        public string? CustomerEmail { get; set; }
    }
}
