using System;
using System.Collections.Generic;

namespace ERP.API.Models
{
    public class DemoGridResult
    {
        public int TotalRecords { get; set; }
        public int Id { get; set; }
        public int? UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? UserUpdated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? UserId { get; set; }
        public DateTime? DemoDate { get; set; }
        public string? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? DemoName { get; set; }
        public string? DemoContact { get; set; }
        public string? DemoApproach { get; set; }
        public string? DemoOutcome { get; set; }
        public string? DemoFeedback { get; set; }
        public string? Comments { get; set; }
        public int? OpportunityId { get; set; }
        public int? PresenterId { get; set; }
        public string? PresenterName { get; set; }
        public int? AddressId { get; set; }
        public int? CustomerId { get; set; }
        public string? OpportunityName { get; set; }
        public string? AddressDetails { get; set; }
        public string? UserCreatedName { get; set; }
        public string? UserUpdatedName { get; set; }
    }
}
