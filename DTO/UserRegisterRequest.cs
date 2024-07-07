namespace NewsAggregation.DTO
{
    public class UserRegisterRequest
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateTime FirstLogin { get; set; }
        public DateTime LastLogin { get; set; }
        public string? ConnectingIp { get; set; }
        public DateTime? Birthdate { get; set; }
        public string? Role { get; set; }
    }
}
