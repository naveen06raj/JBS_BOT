using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.API.Models
{
    [Table("sales_employees")]
    public class SalesEmployee : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int? Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("mobile_no")]
        [MaxLength(15)]
        [Phone]
        public string? MobileNo { get; set; }

        [Column("employee_id")]
        [MaxLength(255)]
        public string? EmployeeId { get; set; }

        [Column("department")]
        [MaxLength(255)]
        public string? Department { get; set; }

        [Column("designation")]
        [MaxLength(255)]
        public string? Designation { get; set; }

        [Column("isactive")]
        public bool IsActive { get; set; } = true;
    }
}
