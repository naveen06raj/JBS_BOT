using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.API.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        [Column("date_created")]  
        public DateTime? DateCreated { get; set; }

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}