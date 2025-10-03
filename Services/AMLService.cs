using BankingTransactionApi.Models;

namespace BankingTransactionApi.Services
{
    public class AMLService
    {
        private readonly ILogger<AMLService> _logger;

        public AMLService(ILogger<AMLService> logger)
        {
            _logger = logger;
        }

        public AMLResult ScreenTransaction(Transaction transaction, Account fromAccount, Account toAccount)
        {
            var riskFactors = new List<RiskFactor>();
            var riskScore = 0;

            // Rule 1: Large Transaction Monitoring (CTR threshold)
            if (transaction.Amount > 10000) // $10,000 CTR requirement
            {
                riskFactors.Add(RiskFactor.LargeTransaction);
                riskScore += 30;
                _logger.LogWarning($"Large transaction detected: {transaction.Amount}");
            }

            // Rule 2: Rapid Successive Transactions
            // This would require transaction history - placeholder for now
            if (transaction.Amount > 5000)
            {
                riskFactors.Add(RiskFactor.ModerateAmount);
                riskScore += 15;
            }

            // Rule 3: Round Amounts (common in structuring)
            if (IsRoundAmount(transaction.Amount))
            {
                riskFactors.Add(RiskFactor.RoundAmount);
                riskScore += 10;
            }

            // Determine risk level
            var riskLevel = riskScore switch
            {
                >= 30 => RiskLevel.High,
                >= 15 => RiskLevel.Medium,
                _ => RiskLevel.Low
            };

            return new AMLResult
            {
                RiskLevel = riskLevel,
                RiskFactors = riskFactors,
                RiskScore = riskScore,
                IsSuspicious = riskLevel == RiskLevel.High,
                RequiredAction = riskLevel == RiskLevel.High ? "Escalate to compliance officer" : "None"
            };
        }

        private bool IsRoundAmount(decimal amount)
        {
            // Check if amount is round (ends with .00)
            return amount == Math.Round(amount);
        }
    }

    public class AMLResult
    {
        public RiskLevel RiskLevel { get; set; }
        public List<RiskFactor> RiskFactors { get; set; } = new();
        public int RiskScore { get; set; }
        public bool IsSuspicious { get; set; }
        public string RequiredAction { get; set; } = string.Empty;
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }

    public enum RiskFactor
    {
        LargeTransaction,    // > $10,000
        ModerateAmount,      // > $5,000
        RoundAmount,         // Even dollar amounts
        RapidTransactions,   // Multiple quick transactions
        HighRiskCountry,     // Transactions with sanctioned countries
        PEPInvolved          // Politically Exposed Person
    }
}