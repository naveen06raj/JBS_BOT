using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.API.Models
{    
    [Table("sales_quotations")]
    public class SalesQuotation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }

        [MaxLength(255)]
        public string? Version { get; set; }

        [MaxLength(255)]
        public string? Terms { get; set; }

        public DateTime? ValidTill { get; set; }

        [MaxLength(255)]
        public string? QuotationFor { get; set; }

        [MaxLength(255)]
        public string? Status { get; set; }

        [MaxLength(255)]
        public string? LostReason { get; set; }

        public int CustomerId { get; set; }

        [MaxLength(255)]
        [Required(ErrorMessage = "CustomerName is required")]
        public string? CustomerName { get; set; }

        [MaxLength(255)]
        public string? QuotationType { get; set; }

        [Required(ErrorMessage = "QuotationDate is required")]
        public DateTime? QuotationDate { get; set; }

        public int OpportunityId { get; set; }

        [MaxLength(255)]
        [Required(ErrorMessage = "OrderType is required")]
        public string? OrderType { get; set; }

        [MaxLength(255)]
        public string? Comments { get; set; }

        [MaxLength(255)]
        public string? DeliveryWithin { get; set; }

        [MaxLength(255)]
        public string? DeliveryAfter { get; set; }

        [Column("is_active")]
        [Required]
        public bool IsActive { get; set; }

        [MaxLength(255)]
        public string? QuotationId { get; set; }

        public int? LeadId { get; set; }

        [MaxLength(255)]
        public string? Taxes { get; set; }

        [MaxLength(255)]
        public string? Delivery { get; set; }

        [MaxLength(255)]
        public string? Payment { get; set; }

        [MaxLength(255)]
        public string? Warranty { get; set; }

        [MaxLength(255)]
        public string? FreightCharge { get; set; }

        public bool? IsCurrent { get; set; }

        [Column("expire_date")]
        public DateTime? ExpireDate { get; set; }

        [MaxLength(255)]
        public string? Territory { get; set; }

        [Column("expected_completion")]
        public DateTime? ExpectedCompletion { get; set; }

        [Column("delivery_prepare_after")]
        [MaxLength(255)]
        public string? DeliveryPrepareAfter { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(255)]
        public string? Contact { get; set; }

        [MaxLength(255)]
        public string? SelectedTaxes { get; set; }

        [MaxLength(255)]
        public string? SelectedFreightCharges { get; set; }

        [MaxLength(255)]
        public string? SelectedDelivery { get; set; }

        [MaxLength(255)]
        public string? SelectedPayment { get; set; }

        [MaxLength(255)]
        public string? SelectedWarranty { get; set; }

        public bool? LatestQuotation { get; set; }

        public bool? Published { get; set; }

        [MaxLength(255)]
        public string? CurrentModifierName { get; set; }

        // Navigation Properties
        [ForeignKey("UserCreated")]
        public virtual User? Creator { get; set; }

        [ForeignKey("UserUpdated")]
        public virtual User? Updater { get; set; }

        [ForeignKey("OpportunityId")]
        public virtual SalesOpportunity? Opportunity { get; set; }

        [ForeignKey("CustomerId")]
        public virtual SalesCustomer? Customer { get; set; }

        [ForeignKey("ParentSalesQuotationsId")]
        public virtual SalesQuotation? ParentQuotation { get; set; }

        [ForeignKey("LeadId")]
        public virtual SalesLead? Lead { get; set; }
        
        // Foreign key navigation properties
        [ForeignKey("SalesLead")]
        public int? SalesLeadsId { get; set; }

        [ForeignKey("SalesOpportunity")]  
        public int? SalesOpportunitiesId { get; set; }

        [ForeignKey("SalesContact")]
        public int? SalesContactsId { get; set; }

        [ForeignKey("SalesAddress")]
        public int? SalesAddressesId { get; set; }

        [ForeignKey("SalesRepresentative")]
        public int? SalesRepresentativesId { get; set; }

        [ForeignKey("ParentQuotation")]
        public int? ParentSalesQuotationsId { get; set; }

        [ForeignKey("CopyFromQuotation")]
        public int? CopyFromSalesQuotationsId { get; set; }

        [ForeignKey("CurrentModifierEmployee")]
        public int? CurrentModifierEmployeesId { get; set; }
    }
}
