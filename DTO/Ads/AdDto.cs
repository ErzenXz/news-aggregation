namespace NewsAggregation.DTO.Ads
{
    public class AdDto
    {
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string? Description { get; set; }
        public DateTime ValidUntil { get; set; }
        public int Views { get; set; }
        public int Clicks { get; set; }
    }
}
