using BankingTransactionApi.Models;
using BankingTransactionApi.Data;
using BankingTransactionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankingTransactionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly BankingContext _context;
        private readonly AccountGenerationService _accountGenerator;

        public AuthController(
            IConfiguration configuration,
            BankingContext context,
            AccountGenerationService accountGenerator)
        {
            _configuration = configuration;
            _context = context;
            _accountGenerator = accountGenerator;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegistrationResponse>> Register([FromBody] RegisterRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Console.WriteLine($"Starting registration for: {request.Username}");

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
                {
                    Console.WriteLine("User already exists");
                    return BadRequest("Username or email already exists");
                }

                // 1. Create User
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                Console.WriteLine("Adding user to context...");
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // Get UserId
                Console.WriteLine($"User created with ID: {user.Id}");

                // 2. Generate Banking Identifiers
                Console.WriteLine("Generating banking identifiers...");
                var customerId = _accountGenerator.GenerateCustomerId(user.Id);
                var generatedAccountNumber = await _accountGenerator.GenerateAccountNumber();
                var routingNumber = _accountGenerator.GenerateRoutingNumber();
                var accountName = _accountGenerator.GenerateAccountName(request.Username);

                Console.WriteLine($"Generated - CustomerId: {customerId}, Account: {generatedAccountNumber}");

                // Update user with customer ID
                user.CustomerId = customerId;

                // 3. Create Default Account
                var defaultAccount = new Account
                {
                    AccountNumber = generatedAccountNumber,
                    AccountHolderName = request.Username,
                    Balance = 0.00m,
                    DailyTransferLimit = 5000.00m,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                Console.WriteLine("Adding account to context...");
                _context.Accounts.Add(defaultAccount);
                await _context.SaveChangesAsync();

                // 4. Commit transaction
                Console.WriteLine("Committing transaction...");
                await transaction.CommitAsync();

                // 5. Log the account creation
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "AccountRegistration",
                    Username = user.Username,
                    Details = $"New customer registered: {customerId}, Account: {generatedAccountNumber}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                Console.WriteLine("Registration completed successfully");
                return Ok(new RegistrationResponse
                {
                    Message = "Bank account created successfully",
                    CustomerId = customerId,
                    AccountNumber = generatedAccountNumber,
                    RoutingNumber = routingNumber,
                    AccountName = accountName,
                    UserId = user.Id,
                    AccountOpened = DateTime.UtcNow,
                    InitialBalance = 0.00m,
                    DailyTransferLimit = 5000.00m
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                return StatusCode(500, $"Account creation failed: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                var token = GenerateJwtToken(user.Username);
                return Ok(new AuthResponse { Token = token, Expires = DateTime.Now.AddMinutes(60) });
            }

            return Unauthorized("Invalid credentials");
        }

        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    

        [HttpGet("test-account-generation")]
        public async Task<IActionResult> TestAccountGeneration()
        {
            try
            {
                var testNumber = await _accountGenerator.GenerateAccountNumber();
                return Ok(new { accountNumber = testNumber });
            }
            catch (Exception ex)
            {
                return BadRequest($"Account generation failed: {ex.Message}");
            }
        }
    }
    }