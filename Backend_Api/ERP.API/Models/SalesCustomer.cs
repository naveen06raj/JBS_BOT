using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.API.Models
{
    [Table("sales_customers")]
    public class SalesCustomer : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int? Id { get; set; }

        [Column("customer_id")]
        [MaxLength(255)]
        public string? CustomerId { get; set; }

        [Required]
        [Column("customer_name")]
        [MaxLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Column("customer_type")]
        [MaxLength(255)]
        public string? CustomerType { get; set; }

        [Column("contact_name")]
        [MaxLength(255)]
        public string? ContactName { get; set; }

        [Column("contact_mobile_no")]
        [MaxLength(255)]
        [Phone]
        public string? ContactMobileNo { get; set; }

        [Column("email")]
        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("land_line_no")]
        [MaxLength(15)]
        public string? LandLineNo { get; set; }

        [Column("fax")]
        [MaxLength(15)]
        public string? Fax { get; set; }

        [Column("website")]
        [MaxLength(100)]
        public string? Website { get; set; }

        [Column("door_no")]
        [MaxLength(5)]
        public string? DoorNo { get; set; }

        [Column("street")]
        [MaxLength(250)]
        public string? Street { get; set; }

        [Column("landmark")]
        [MaxLength(50)]
        public string? Landmark { get; set; }

        [Column("territory")]
        [MaxLength(255)]
        public string? Territory { get; set; }

        [Column("area")]
        [MaxLength(255)]
        public string? Area { get; set; }

        [Column("city")]
        [MaxLength(255)]
        public string? City { get; set; }

        [Column("pincode")]
        [MaxLength(255)]
        public string? Pincode { get; set; }

        [Column("district")]
        [MaxLength(255)]
        public string? District { get; set; }

        [Column("state")]
        [MaxLength(255)]
        public string? State { get; set; }

        [Column("country")]
        [MaxLength(255)]
        public string? Country { get; set; }

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("comments")]
        public string? Comments { get; set; }
    }
}
