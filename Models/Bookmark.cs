using NewsAggregation.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace News_aggregation.Entities
{
    public class Bookmark
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public Guid ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public Article Article { get; set; }
    }
}
