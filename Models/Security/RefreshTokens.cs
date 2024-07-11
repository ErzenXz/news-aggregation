using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models.Security
{
    public class RefreshTokens
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public required string Token { get; set; }
        public DateTime Expires { get; set; }
        public int TokenVersion { get; set; }


        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= Expires;

        public DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }

        [NotMapped]
        public bool IsActive => Revoked == null && !IsExpired;

        public string? UserAgent { get; set; }
        public string? DeviceName { get; set; }
        public string? RevocationReason { get; set; }
        public DateTime? LastUsed { get; set; }
    }


}
