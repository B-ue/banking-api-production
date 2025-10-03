namespace BankingTransactionApi.Models
{
    public class RegistrationResponse
    {
        public string AccountName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal DailyTransferLimit { get; set; }
        public DateTime AccountOpened { get; set; }
    }
}
