using BankingTransactionApi.Data;
using BankingTransactionApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly BankingContext _context;

    public AdminController(BankingContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        return await _context.Users.Include(u => u.Accounts).ToListAsync();
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<Transaction>>> GetAllTransactions()
    {
        return await _context.Transactions
            .Include(t => t.FromAccountId)
            .Include(t => t.ToAccountId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    [HttpPost("users/{userId}/lock")]
    public async Task<IActionResult> LockUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok("User locked successfully");
    }
}