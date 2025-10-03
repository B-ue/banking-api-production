using BankingTransactionApi.Models;
using BankingTransactionApi.Data;
using BankingTransactionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BankingTransactionApi.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly AccountGenerationService _accountGenerator;
        private readonly IConfiguration _configuration;

        public AdminController(BankingContext context, AccountGenerationService accountGenerator, IConfiguration configuration)
        {
            _context = context;
            _accountGenerator = accountGenerator;
            _configuration = configuration;
        }

        // SECURE ADMIN SETUP
        [AllowAnonymous]
        [HttpPost("setup-super-admin")]
        public async Task<IActionResult> SetupSuperAdmin([FromBody] RegisterRequest request)
        {
            // Check if super admin already exists
            var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "SuperAdmin");
            if (existingAdmin != null)
                return BadRequest("Super admin already exists. Cannot create another.");

            // Verify this is the configured super admin
            var allowedEmails = _configuration.GetSection("AdminSettings:AllowedAdminEmails").Get<string[]>();
            if (allowedEmails == null || !allowedEmails.Contains(request.Email))
                return Unauthorized("Not authorized to create super admin account");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var superAdmin = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "SuperAdmin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(superAdmin);
                await _context.SaveChangesAsync();

                var customerId = _accountGenerator.GenerateCustomerId(superAdmin.Id);
                superAdmin.CustomerId = customerId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Super admin setup completed successfully" });
            }
            catch (Exception )
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Super admin setup failed");
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] RegisterRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
                    return BadRequest("User already exists");

                var admin = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(admin);
                await _context.SaveChangesAsync();

                var customerId = _accountGenerator.GenerateCustomerId(admin.Id);
                admin.CustomerId = customerId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Admin user created successfully" });
            }
            catch (Exception )
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Admin creation failed");
            }
        }

        // BUSINESS INTELLIGENCE DASHBOARD
        [HttpGet("business-intelligence")]
        public async Task<IActionResult> GetBusinessIntelligence()
        {
            var today = DateTime.UtcNow.Date;

            var metrics = new
            {
                // Financial Metrics
                TotalDeposits = await _context.Accounts.SumAsync(a => a.Balance),
                DailyTransactionVolume = await _context.Transactions
                    .Where(t => t.Timestamp.Date == today)
                    .SumAsync(t => t.Amount),
                AverageTransactionSize = await _context.Transactions
                    .Where(t => t.Timestamp.Date == today)
                    .AverageAsync(t => (double?)t.Amount) ?? 0,

                // Customer Metrics  
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                NewCustomersToday = await _context.Users
                    .CountAsync(u => u.CreatedAt.Date == today && u.Role == "Customer"),
                ActiveCustomers = await _context.Users
                    .CountAsync(u => u.IsActive && u.Role == "Customer"),

                // Risk & Compliance
                FlaggedTransactions = await _context.Transactions.CountAsync(t => t.IsFlagged),
                HighRiskTransactions = await _context.Transactions
                    .CountAsync(t => t.RiskLevel == "High"),

                // System Health
                TotalTransactions = await _context.Transactions.CountAsync(),
                SystemUptime = "99.9%"
            };

            return Ok(metrics);
        }

        [HttpGet("customer/{customerId}/profile")]
        public async Task<IActionResult> GetCustomerProfile(string customerId)
        {
            var customer = await _context.Users
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.CustomerId == customerId && u.Role == "Customer");

            if (customer == null) return NotFound();

            var transactionHistory = await _context.Transactions
                .Where(t => t.FromAccount.UserId == customer.Id || t.ToAccount.UserId == customer.Id)
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .OrderByDescending(t => t.Timestamp)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                Customer = new
                {
                    customer.CustomerId,
                    customer.Username,
                    customer.Email,
                    customer.CreatedAt,
                    customer.IsActive
                },
                Accounts = customer.Accounts.Select(a => new
                {
                    a.AccountNumber,
                    a.Balance,
                    a.DailyTransferLimit
                }),
                RecentTransactions = transactionHistory.Select(t => new
                {
                    t.TransactionId,
                    t.Amount,
                    FromAccount = t.FromAccount?.AccountNumber,
                    ToAccount = t.ToAccount?.AccountNumber,
                    t.Timestamp,
                    t.RiskLevel,
                    t.IsFlagged
                })
            });
        }

        [HttpPost("transactions/{transactionId}/review")]
        public async Task<IActionResult> ReviewFlaggedTransaction(int transactionId, [FromBody] ReviewRequest request)
        {
            var transaction = await _context.Transactions
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.IsFlagged);

            if (transaction == null) return NotFound();

            // Update transaction based on review
            transaction.IsFlagged = request.Decision != "Approve";
            transaction.ScreeningResult = $"Reviewed by admin. Decision: {request.Decision}. Notes: {request.Notes}";

            await _context.SaveChangesAsync();

            // Log admin action
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TransactionReview",
                Username = User.Identity.Name,
                Details = $"Reviewed transaction {transactionId}. Decision: {request.Decision}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Transaction reviewed successfully" });
        }

        [HttpGet("compliance-report")]
        public async Task<IActionResult> GenerateComplianceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var largeTransactions = await _context.Transactions
                .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate && t.Amount > 10000)
                .Include(t => t.FromAccount)
                .ThenInclude(a => a.User)
                .ToListAsync();

            var flaggedActivity = await _context.Transactions
                .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate && t.IsFlagged)
                .CountAsync();

            return Ok(new
            {
                ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                LargeTransactions = largeTransactions.Select(t => new
                {
                    t.TransactionId,
                    t.Amount,
                    FromCustomer = t.FromAccount.User.CustomerId,
                    ToAccount = t.ToAccount.AccountNumber,
                    t.Timestamp,
                    t.RiskLevel
                }),
                TotalFlaggedTransactions = flaggedActivity,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = User.Identity.Name
            });
        }

        [HttpGet("suspicious-transactions")]
        public async Task<IActionResult> GetSuspiciousTransactions()
        {
            var transactions = await _context.Transactions
                .Where(t => t.IsFlagged)
                .Include(t => t.FromAccount)
                .ThenInclude(a => a.User)
                .Include(t => t.ToAccount)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            return Ok(transactions.Select(t => new
            {
                t.Id,
                t.TransactionId,
                t.Amount,
                FromCustomer = t.FromAccount.User.CustomerId,
                ToAccount = t.ToAccount.AccountNumber,
                t.Timestamp,
                t.RiskLevel,
                t.ScreeningResult
            }));
        }
    }
}