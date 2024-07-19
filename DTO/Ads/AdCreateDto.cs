using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.DTO.Ads
{
    public class AdCreateDto
    {
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string? Description { get; set; }
        public DateTime ValidUntil { get; set; }
    }
}
