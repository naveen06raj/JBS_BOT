using System;
using System.ComponentModel.DataAnnotations;

namespace ERP.API.Models
{
    public class SalesOrder
    {
        public int Id { get; set; }
          [StringLength(20)]
        public string OrderId { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        [Required]        public DateTimeOffset OrderDate { get; set; }
        
        public DateTimeOffset? ExpectedDeliveryDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; }
        
        public int? QuotationId { get; set; }
        
        [StringLength(50)]
        public string PoId { get; set; }
        
        public DateTimeOffset? AcceptanceDate { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public decimal TaxAmount { get; set; }
        
        public decimal GrandTotal { get; set; }
        
        public string Notes { get; set; }
        
        public int? UserCreated { get; set; }
          public DateTimeOffset? DateCreated { get; set; }
        
        public int? UserUpdated { get; set; }
        
        public DateTimeOffset? DateUpdated { get; set; }
    }

    public class SalesOrderGrid
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; }
        public string PoId { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
