using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ERP.API.Models
{
    public class SalesDemoGridRequest
    {
        public string? SearchText { get; set; }
        public string[]? CustomerNames { get; set; }
        public string[]? Statuses { get; set; }
        public string[]? DemoApproaches { get; set; }
        public string[]? DemoOutcomes { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;
        
        [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0")]
        public int PageSize { get; set; } = 10;
        
        public string OrderBy { get; set; } = "dateCreated";
        
        [RegularExpression("^(ASC|DESC)$", ErrorMessage = "Order direction must be either 'ASC' or 'DESC'")]
        public string OrderDirection { get; set; } = "DESC";
    }
}
