using System.ComponentModel.DataAnnotations.Schema;
using News_aggregation.Entities;

namespace NewsAggregation.DTO.Article;

public class ArticleCreateDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Tags { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public Guid SourceId { get; set; }
    public DateTime PublishedAt { get; set; }
    public Guid AuthorId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}