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
        [Column(TypeName = "decimal(18,2)")]  // ONLY this attribute for Amount
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string Status { get; set; } = "Completed";

        [Column(TypeName = "nvarchar(255)")]
        public string Description { get; set; }
    }
}