using NewsAggregation.Models;

namespace NewsAggregation.DTO.Subscriptions
{
    public class SubscriptionCreateDto
    {
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
        
    }
}
