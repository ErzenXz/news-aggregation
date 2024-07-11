using NewsAggregation.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace News_aggregation.Entities
{
    public class Article
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string SourceURL { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Author { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public int Views { get; set; }



        public ICollection<Comment> Comments { get; set; }
        public ICollection<ArticleTag> ArticleTags { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
    }
}
