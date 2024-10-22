﻿namespace NewsAggregation.DTO
{
    public class UserRegisterRequest
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateTime? Birthdate { get; set; }
        public string? Language { get; set; }
        public string? TimeZone { get; set; }

    }
}
