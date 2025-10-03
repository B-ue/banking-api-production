using BankingTransactionApi.Data;
using BankingTransactionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingTransactionApi.Services
{
    public class AMLService
    {
        private readonly BankingContext _context;
        private readonly Random _random = new();

        public AMLService(BankingContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAccountNumber()
        {
            string accountNumber;
            bool isUnique;

            do
            {
                accountNumber = $"021{_random.Next(1000000, 9999999):D7}";
                isUnique = !await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
            }
            while (!isUnique);

            return accountNumber;
        }

        public string GenerateCustomerId(int userId)
        {
            return $"CUS{DateTime.UtcNow:yyyyMMddHHmmss}{userId:D6}";
        }

        public string GenerateRoutingNumber()
        {
            return "021000021";
        }

        public string GenerateAccountName(string username)
        {
            return $"{username}'s Primary Account";
        }

        public AMLResult ScreenTransaction(Transaction transaction, Account fromAccount, Account toAccount)
        {
            var riskFactors = new List<RiskFactor>();
            var riskScore = 0;

            // Rule 1: Large Transaction Monitoring (CTR threshold)
            if (transaction.Amount > 10000)
            {
                riskFactors.Add(RiskFactor.LargeTransaction);
                riskScore += 30;
            }

            // Rule 2: Round Amounts (common in structuring)
            if (IsRoundAmount(transaction.Amount))
            {
                riskFactors.Add(RiskFactor.RoundAmount);
                riskScore += 10;
            }

            // Determine risk level
            var riskLevel = riskScore switch
            {
                >= 30 => "High",
                >= 15 => "Medium",
                _ => "Low"
            };

            var isFlagged = riskLevel == "High";

            return new AMLResult
            {
                RiskLevel = riskLevel,
                RiskFactors = riskFactors,
                RiskScore = riskScore,
                IsSuspicious = isFlagged,
                RequiredAction = isFlagged ? "Escalate to compliance officer" : "None"
            };
        }

        private bool IsRoundAmount(decimal amount)
        {
            return amount == Math.Round(amount);
        }
    }

    public class AMLResult
    {
        public string RiskLevel { get; set; } = string.Empty;
        public List<RiskFactor> RiskFactors { get; set; } = new();
        public int RiskScore { get; set; }
        public bool IsSuspicious { get; set; }
        public string RequiredAction { get; set; } = string.Empty;
    }

    public enum RiskFactor
    {
        LargeTransaction,
        ModerateAmount,
        RoundAmount,
        RapidTransactions,
        HighRiskCountry,
        PEPInvolved
    }
}