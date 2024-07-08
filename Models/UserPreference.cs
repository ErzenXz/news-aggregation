using NewsAggregation.Models;

namespace News_aggregation.Entities
{
    public class UserPreference
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
