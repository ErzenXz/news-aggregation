namespace NewsAggregation.DTO
{
    internal class ActiveSession
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Expires { get; set; }
        public string? UserAgent { get; set; }
        public object? IpAddress { get; set; }
        public bool? IsActive { get; set; }
    }
}