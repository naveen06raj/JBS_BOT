using System.Collections.Generic;

namespace ERP.API.Models.DealGrid
{
    public class DealGridRequest
    {
        public string? SearchText { get; set; }
        public List<string>? CustomerNames { get; set; }
        public List<string>? Statuses { get; set; }
        public List<string>? DealIds { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; }
    }
}
