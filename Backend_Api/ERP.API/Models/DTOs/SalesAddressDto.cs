using System;
using System.ComponentModel.DataAnnotations;

namespace ERP.API.Models.DTOs
{    public class SalesAddressDto
    {
        public int? Id { get; set; }

        // Audit Fields
        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }

        // Basic Information
        public string? ContactName { get; set; }
        public string? Type { get; set; }
        public bool? IsActive { get; set; }

        // Location Details
        public string? Block { get; set; }
        public string? Department { get; set; }
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }

        // Additional Details
        public string? OpportunityId { get; set; }
        public string? DoorNo { get; set; }
        public string? Street { get; set; }
        public string? Landmark { get; set; }
        public bool? IsDefault { get; set; }
        public int? SalesLeadId { get; set; }
    }
}