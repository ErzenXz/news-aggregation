using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using News_aggregation.Entities;
using NewsAggregation.Models.Security;
using NewsAggregation.Models.Stats;

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

        public string? TotpSecret { get; set; }
        public string? BackupCodes { get; set; }

        public string? ExternalProvider { get; set; }
        public string? ExternalUserId { get; set; }
        public bool IsExternal { get; set; } = false;



        // Personalization
        public string? TimeZone { get; set; } 
        public string? Language { get; set; }


        public ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();
        public ICollection<Subscriptions> Subscriptions { get; set; } = new List<Subscriptions>();



        // For Auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
