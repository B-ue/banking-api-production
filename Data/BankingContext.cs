using Microsoft.EntityFrameworkCore;
using BankingTransactionApi.Models;
using BankingTransactionApi.Controllers;


namespace BankingTransactionApi.Data
{
    public class BankingContext : DbContext
    {
        public BankingContext(DbContextOptions<BankingContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}
