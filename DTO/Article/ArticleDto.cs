using System.ComponentModel.DataAnnotations.Schema;
using News_aggregation.Entities;

namespace NewsAggregation.DTO.Article;

public class ArticleDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Author { get; set; }
    public int CategoryId { get; set; }

    public string Tags { get; set; }
   
    public int likes { get; set; }
}