namespace NewsAggregation.Models.Security
{
    public class AuthLogs
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Date { get; set; }
        public string? Result { get; set; }
    }
}
