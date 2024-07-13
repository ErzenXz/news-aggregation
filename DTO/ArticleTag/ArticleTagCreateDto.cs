namespace NewsAggregation.DTO.ArticleTag;

public class ArticleTagCreateDto
{
    public Guid ArticleId { get; set; }
    public int TagId { get; set; }
}