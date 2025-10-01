using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingTransactionApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public required string Username { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public required string Email { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public required string PasswordHash { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Account>? Accounts { get; set; }
    }
}