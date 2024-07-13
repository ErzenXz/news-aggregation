using System.ComponentModel.DataAnnotations.Schema;
using News_aggregation.Entities;

namespace NewsAggregation.DTO.Article;

public class ArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public string SourceURL { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Author { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UserId { get; set; }
    public int Views { get; set; }
}