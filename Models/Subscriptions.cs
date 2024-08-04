namespace NewsAggregation.Models
{
    public class Subscriptions
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        
        public string StripeSubscriptionId { get; set; }
        
        public string StripeCustomerId { get; set; }
        public string StripePriceId { get; set; }
        public bool IsPaid { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastPaymentDate { get; set; }

    }
}
