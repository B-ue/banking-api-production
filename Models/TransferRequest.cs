using System.ComponentModel.DataAnnotations;

namespace BankingTransactionApi.Models
{
    public class TransferRequest
    {
        [Required]
        public required string FromAccount { get; set; }
        [Required]
        public required string ToAccount { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public string? Description { get; set; }
    }
}