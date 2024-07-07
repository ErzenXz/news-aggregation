using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NewsAggregation.Models.Security
{
    public class IpMitigations
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string IpAddress { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
