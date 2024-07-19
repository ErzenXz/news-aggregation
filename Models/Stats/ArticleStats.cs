using News_aggregation.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregation.Models.Stats
{
    public class ArticleStats
    {
        public Guid Id { get; set; }

        [ForeignKey("ArticleId")]
        public Guid ArticleId { get; set; }
        public Article Article { get; set; }

        [ForeignKey("UserId")]
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTime ViewTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

    }
}
