namespace NewsAggregation.DTO.Payments
{
    public class PaymentCreateDto
    {

        public Guid SubscriptionId { get; set; }
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
