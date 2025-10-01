using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingTransactionApi.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string AccountHolderName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyTransferLimit { get; set; } = 5000;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key - NOT required
        public int UserId { get; set; }

        // Navigation property - NOT required
        public virtual User? User { get; set; }
    }
}