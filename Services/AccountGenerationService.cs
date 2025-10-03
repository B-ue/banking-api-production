using BankingTransactionApi.Data;
using Microsoft.EntityFrameworkCore;



namespace BankingTransactionApi.Services
{
    public class AccountGenerationService
    {
        private readonly BankingContext _context;
        private readonly Random _random = new();

        public AccountGenerationService(BankingContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAccountNumber()
        {
            string accountNumber;
            bool isUnique;

            do
            {
                // Format: 6-digit bank code + 10-digit account number
                accountNumber = $"021000{_random.Next(1000000000, 1999999999)}";
                isUnique = !await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
            }
            while (!isUnique);

            return accountNumber;
        }

        public string GenerateCustomerId(int userId)
        {
            // Format: CUS + timestamp + user ID
            return $"CUS{DateTime.UtcNow:yyyyMMddHHmmss}{userId:D6}";
        }

        public string GenerateRoutingNumber()
        {
            // Standard US routing number format
            return "021000021"; // This would be your bank's actual routing number
        }

        public string GenerateAccountName(string AccountHolderName)
        {
            return $"{AccountHolderName}";
        }
    }
}