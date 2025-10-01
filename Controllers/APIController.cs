using Microsoft.AspNetCore.Mvc;
using BankingTransactionApi.Models;
using BankingTransactionApi.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingTransactionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly BankingContext _context;

        public TransactionsController(BankingContext context)
        {
            _context = context;
        }

        // Original simple transfer (keep for reference)
        [HttpPost("simple-transfer")]
        public string SimpleTransfer(string from, string to, decimal amount)
        {
            string message = $"Transferring {amount} from {from} to {to}";
            decimal fromBalance = 1000 - amount;
            decimal toBalance = 500 + amount;
            return message + $" - New balances: From={fromBalance}, To={toBalance}";
        }

        // Original v2 transfer (keep for reference)
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

        // NEW: Database-powered account creation
        [HttpPost("create-account")]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return Ok(account);
        }

        // NEW: Get all accounts from database
        [HttpGet("accounts")]
        public async Task<ActionResult<List<Account>>> GetAccounts()
        {
            return await _context.Accounts.ToListAsync();
        }

        // NEW: Database-powered money transfer
        [HttpPost("db-transfer")]
        public async Task<ActionResult<string>> DbTransfer([FromBody] TransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Find accounts in database
                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == request.FromAccount);
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccount);

                if (fromAccount == null || toAccount == null)
                    return BadRequest("One or both accounts not found");

                if (fromAccount.Balance < request.Amount)
                    return BadRequest("Insufficient funds");

                // Perform transfer
                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;

                // Record transaction
                var transactionRecord = new Transaction
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountId = toAccount.Id,
                    Amount = request.Amount,
                    Description = request.Description
                };

                _context.Transactions.Add(transactionRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok($"Transferred {request.Amount} from {request.FromAccount} to {request.ToAccount}");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}