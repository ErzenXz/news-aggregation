using NewsAggregation.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace News_aggregation.Entities
{
    public class Bookmark
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("ArticleId")]
        public Guid ArticleId { get; set; }
        public Article Article { get; set; }
    }
}
