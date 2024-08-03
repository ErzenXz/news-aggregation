namespace NewsAggregation.DTO.Plans
{
    public class PlanCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Currency { get; set; }
        public string StripeProductId { get; set; }
        public string StripePriceId { get; set; }

    }
}
