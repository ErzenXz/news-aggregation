using System.ComponentModel.DataAnnotations.Schema;
using NewsAggregation.Models;

namespace News_aggregation.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("ArticleId")]
        public Guid ArticleId { get; set; }
        public Article Article { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
