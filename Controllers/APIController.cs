using BankingTransactionApi.Models;
using System.Net;
using Microsoft.Extensions.Logging;
using BankingTransactionApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingTransactionApi.Controllers
{
    [EnableRateLimiting("Fixed")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(BankingContext context, ILogger<TransactionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Original simple transfer (keep for reference)
        [AllowAnonymous] // Remove in production
        [HttpPost("simple-transfer")]
        public string SimpleTransfer(string from, string to, decimal amount)
        {
            string message = $"Transferring {amount} from {from} to {to}";
            decimal fromBalance = 1000 - amount;
            decimal toBalance = 500 + amount;
            return message + $" - New balances: From={fromBalance}, To={toBalance}";
        }

        // Original v2 transfer (keep for reference)
        [AllowAnonymous] // Remove in production
        [HttpPost("transfer-v2")]
        public string TransferV2(string from, string to, decimal amount)
        {
            var accounts = new List<Account>
            {
                new Account { AccountNumber = "ACC001", Balance = 1000 },
                new Account { AccountNumber = "ACC002", Balance = 500 }
            };

            var fromAccount = accounts.First(a => a.AccountNumber == from);
            var toAccount = accounts.First(a => a.AccountNumber == to);

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            return $"Transferred {amount}. New balances: {fromAccount.Balance}, {toAccount.Balance}";
        }

        // Database-powered account creation
        [HttpPost("create-account")]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized("User not found");

            // Link account to logged-in user
            account.UserId = user.Id;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Audit log
            await LogAction("CreateAccount", $"Created account {account.AccountNumber}");

            return Ok(account);
        }

        // Get user's accounts
        [HttpGet("my-accounts")]
        public async Task<ActionResult<List<Account>>> GetMyAccounts()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound();

            return Ok(user.Accounts);
        }

        // Get all accounts (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("accounts")]
        public async Task<ActionResult<List<Account>>> GetAllAccounts()
        {
            return await _context.Accounts.Include(a => a.User).ToListAsync();
        }

        // Database-powered money transfer
        [HttpPost("transfer")]
        public async Task<ActionResult<string>> Transfer([FromBody] TransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var username = User.Identity?.Name;
                var user = await _context.Users
                    .Include(u => u.Accounts)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return Unauthorized("User not found");

                // Find accounts - ensure user owns the fromAccount
                var fromAccount = user.Accounts.FirstOrDefault(a => a.AccountNumber == request.FromAccount);
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccount);

                if (fromAccount == null)
                    return BadRequest("Source account not found or you don't own it");
                if (toAccount == null)
                    return BadRequest("Destination account not found");

                // Business logic validation
                if (fromAccount.Balance < request.Amount)
                    return BadRequest("Insufficient funds");
                if (request.Amount <= 0)
                    return BadRequest("Amount must be positive");
                if (fromAccount.DailyTransferLimit < request.Amount)
                    return BadRequest("Transfer amount exceeds daily limit");

                // Perform transfer
                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;

                // Record transaction
                var transactionRecord = new Transaction
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountId = toAccount.Id,
                    Amount = request.Amount,
                    Description = request.Description ?? $"Transfer to {toAccount.AccountNumber}",
                    Timestamp = DateTime.UtcNow
                };

                _context.Transactions.Add(transactionRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Audit log
                await LogAction("MoneyTransfer",
                    $"Transferred {request.Amount} from {request.FromAccount} to {request.ToAccount}");

                _logger.LogInformation("Transfer completed: {From} -> {To} : {Amount}",
                    request.FromAccount, request.ToAccount, request.Amount);

                return Ok($"Transferred {request.Amount} from {request.FromAccount} to {request.ToAccount}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transfer failed: {From} -> {To} : {Amount}",
                    request.FromAccount, request.ToAccount, request.Amount);
                throw;
            }
        }

        // User transaction history
        [HttpGet("my-transactions")]
        public async Task<ActionResult<List<Transaction>>> GetMyTransactions()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound();

            var accountIds = user.Accounts?.Select(a => a.Id).ToList();

            var transactions = await _context.Transactions
                .Where(t => accountIds.Contains(t.FromAccountId) || accountIds.Contains(t.ToAccountId))
                .Include(t => t.FromAccountId)
                .Include(t => t.ToAccountId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            return Ok(transactions);
        }

        // Get account balance
        [HttpGet("balance/{accountNumber}")]
        public async Task<ActionResult<decimal>> GetBalance(string accountNumber)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Username == username);

            var account = user?.Accounts?.FirstOrDefault(a => a.AccountNumber == accountNumber);
            if (account == null)
                return NotFound("Account not found or you don't have access");

            return Ok(account.Balance);
        }

        private async Task LogAction(string action, string details)
        {
            var auditLog = new AuditLog  // ← lowercase "a"
            {
                Action = action,
                Username = User.Identity?.Name ?? "Unknown",
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);  // ← lowercase "a"
            await _context.SaveChangesAsync();
        }
    }
}