namespace NewsAggregation.DTO
{
    public class UserUpdateRequest
    {
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime FirstLogin { get; set; }
        public string ConIP { get; set; }
        public DateTime Birthday { get; set; }
        public string? TimeZone { get; set; }
        public string? Language { get; set; }
    }

}
