using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models.Security
{
    public class PasswordChanges
    {
        public Guid Id { get; set; }
        [ForeignKey("UserID")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Date { get; set; }
    }
}
