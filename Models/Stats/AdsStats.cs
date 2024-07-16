namespace NewsAggregation.Models.Stats
{
    public class AdsStats
    {
        public Guid Id { get; set; }
        public Guid AdId { get; set; }
        public Ads Ads { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTime ViewTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
