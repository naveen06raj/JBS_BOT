using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.API.Models
{
    [Table("sales_products")]
    public class SalesProducts : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int? Id { get; set; }


        [Column("qty")]
        public int? Quantity { get; set; }


        [Column("amount")]
        public float? Amount { get; set; }

        [Column("isactive")]
        public bool? IsActive { get; set; }

        [Required]
        [Column("inventory_items_id")]
        public int InventoryItemsId { get; set; }
        [Required]
        [Column("stage_item_id")]
        public long? StageItemId { get; set; }
        [Required]
        [Column("stage")]
        [MaxLength(255)]
        public string? Stage { get; set; } = string.Empty;

        // Navigation properties
        public int? MakeId { get; set; }
        public string? MakeName { get; set; } = string.Empty;

        public int? ModelId { get; set; }
        public string? ModelName { get; set; } = string.Empty;

        public int? ProductId { get; set; }
        public string? ProductName { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; } = string.Empty;

        public string? ItemCode { get; set; } = string.Empty;
        public string? ItemName { get; set; } = string.Empty; [Column("unit_price", TypeName = "decimal(12,2)")]
        public decimal? UnitPrice { get; set; }
    }
}