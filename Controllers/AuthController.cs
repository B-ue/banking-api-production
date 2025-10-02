using BankingTransactionApi.Models;
using BankingTransactionApi.Data;
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
        private readonly BankingContext _context;  // Add private field
        private readonly SigningCredentials credentials;

        // Use constructor injection properly
        public AuthController(IConfiguration configuration, BankingContext context)
        {
            _configuration = configuration;
            _context = context;  // Assign to private field
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Check if user exists - use _context, not bankingContext
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
                return BadRequest("User already exists");

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);  // Use _context
            await _context.SaveChangesAsync();  // Use _context

            return Ok("User registered successfully");
        }

        // Rest of your methods remain the same...
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // REAL user validation now
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
            // rest of method unchanged

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "User")
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}