namespace NewsAggregation.DTO.Payments
{
    public class PaymentCreateDto
    {
        public string? StripeProductId { get; set; }
        public string StripePriceId { get; set; }
        public string StripeCustomerId { get; set; }
    }

}
