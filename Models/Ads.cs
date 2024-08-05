using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models
{
    public class Ads
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }


        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ValidUntil;

        public DateTime CreatedAt { get; set; }
        public DateTime ValidUntil { get; set; }

        public int Views { get; set; }
        public int GuaranteedViews { get; set; }
        public int Clicks { get; set; }

    }
}
