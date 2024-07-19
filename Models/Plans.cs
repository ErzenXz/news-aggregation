﻿namespace NewsAggregation.Models
{
    public class Plans
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set;}
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        ICollection<Subscriptions> Subscriptions { get; set; }

    }
}