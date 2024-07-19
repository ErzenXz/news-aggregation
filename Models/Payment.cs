using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models
{
    public class Payment
    {
        public Guid Id { get; set; }
        
        public Guid SubscriptionId { get; set; }
        [ForeignKey("SubscriptionId")]
        public Subscriptions Subscription { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentGateway { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentDescription { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
