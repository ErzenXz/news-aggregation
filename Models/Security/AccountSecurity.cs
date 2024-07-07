using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NewsAggregation.Models.Security
{
    public class AccountSecurity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long UserId { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public string IpAddress { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

    }
}
