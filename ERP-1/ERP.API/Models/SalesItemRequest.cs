using System.ComponentModel.DataAnnotations;

namespace ERP.API.Models
{
    public class SalesItemRequest
    {
        public int? Id { get; set; }
        public int? Quantity { get; set; }
        public double? Amount { get; set; }
        public int ItemId { get; set; }
        public string? Stage { get; set; }
        public int? StageItemId { get; set; }
        public bool? IsActive { get; set; }
    }
}
