using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingTransactionApi.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Username { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(500)")]
        public string Details { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}