using System;
using System.ComponentModel.DataAnnotations;

namespace ERP.API.Models.DTOs
{
    public class SalesDemoDto
    {
        public int Id { get; set; }
        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }

        // Core Fields
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo name is required")]
        [StringLength(255)]
        public string DemoName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo date is required")]
        public DateTime DemoDate { get; set; }

        [Required(ErrorMessage = "Demo contact is required")]
        [StringLength(255)]
        public string DemoContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo approach is required")]
        [StringLength(255)]
        public string DemoApproach { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DemoOutcome { get; set; }

        [StringLength(255)]
        public string? DemoFeedback { get; set; }

        [StringLength(255)]
        public string? Comments { get; set; }

        // References
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Address ID is required")]
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Opportunity ID is required")]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Presenter ID is required")]
        public int PresenterId { get; set; }
    }

    public class CreateSalesDemoDto
    {
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo name is required")]
        [StringLength(255)]
        public string DemoName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo date is required")]
        public DateTime DemoDate { get; set; }

        [Required(ErrorMessage = "Demo contact is required")]
        [StringLength(255)]
        public string DemoContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo approach is required")]
        [StringLength(255)]
        public string DemoApproach { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DemoOutcome { get; set; }

        [StringLength(255)]
        public string? DemoFeedback { get; set; }

        [StringLength(255)]
        public string? Comments { get; set; }

        // References
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Address ID is required")]
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Opportunity ID is required")]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Presenter ID is required")]
        public int PresenterId { get; set; }
    }

    public class UpdateSalesDemoDto
    {
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo name is required")]
        [StringLength(255)]
        public string DemoName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo date is required")]
        public DateTime DemoDate { get; set; }

        [Required(ErrorMessage = "Demo contact is required")]
        [StringLength(255)]
        public string DemoContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Demo approach is required")]
        [StringLength(255)]
        public string DemoApproach { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DemoOutcome { get; set; }

        [StringLength(255)]
        public string? DemoFeedback { get; set; }

        [StringLength(255)]
        public string? Comments { get; set; }

        // References
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Address ID is required")]
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Opportunity ID is required")]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Presenter ID is required")]
        public int PresenterId { get; set; }
    }
}
