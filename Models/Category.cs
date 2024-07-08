﻿namespace News_aggregation.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public ICollection<Article> Articles { get; set; }
        public ICollection<UserPreference> UserPreferences { get; set; }
    }
}