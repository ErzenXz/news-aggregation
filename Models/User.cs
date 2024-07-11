using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NewsAggregation.Models.Security;

namespace NewsAggregation.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateTime FirstLogin { get; set; }    
        public DateTime LastLogin { get; set; }
        public string? ProfilePicture { get; set; }
        public string? ConnectingIp { get; set; } 
        public DateTime? Birthdate { get; set; }
        public string? Role { get; set; }
        public int TokenVersion { get; set; }

        public bool IsEmailVerified { get; set; }
        public DateTime? PasswordLastChanged { get; set; }
        public bool IsTwoFactorEnabled { get; set; }

        // Personalization
        public string? TimeZone { get; set; } 
        public string? Language { get; set; } 

        public ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();

        // For Auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
