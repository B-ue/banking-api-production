namespace BankingTransactionApi.Models
{
    public class ReviewRequest
    {
        public string Decision { get; set; } = string.Empty; // "Approve", "Reject", "Monitor"
        public string Notes { get; set; } = string.Empty;
    }
}