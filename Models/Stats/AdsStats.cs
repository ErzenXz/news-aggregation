using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models.Stats
{
    public class AdsStats
    {
        public Guid Id { get; set; }
        [ForeignKey("AdId")]
        public Guid AdId { get; set; }
        public Ads Ads { get; set; }

        [ForeignKey("UserId")]
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTime ViewTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public bool IsClicked { get; set; }
    }
}
