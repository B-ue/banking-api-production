using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingTransactionApi.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int FromAccountId { get; set; }

        [Required]
        public int ToAccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(50)")]
        public string Status { get; set; } = "Completed";

        [Column(TypeName = "nvarchar(255)")]
        public string? Description { get; set; }

        
        [Column(TypeName = "nvarchar(20)")]
        public string? RiskLevel { get; set; } 

        public bool IsFlagged { get; set; } = false;

        [Column(TypeName = "nvarchar(500)")]
        public string? ScreeningResult { get; set; }

        // Navigation properties
        public virtual Account? FromAccount { get; set; }
        public virtual Account? ToAccount { get; set; }
    }
}