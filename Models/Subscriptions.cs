namespace NewsAggregation.Models
{
    public class Subscriptions
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public Guid PlanId { get; set; }
        public Plans Plan { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
