using NewsAggregation.Models;

namespace NewsAggregation.DTO.Article
{
    public class ArticleUpdateDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public Guid SourceId { get; set; }
        public Guid AuthorId { get; set; }
        public int CategoryId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

