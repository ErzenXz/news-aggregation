namespace NewsAggregation.Models
{
    public class UserHistory
    {
        public Guid Id { get; set; }
        public Guid ? ArticleId { get; set; }
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Tags { get; set; }
        public DateTime Date { get; set; }
    }
}
