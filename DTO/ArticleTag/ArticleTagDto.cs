namespace NewsAggregation.DTO.ArticleTag;

public class ArticleTagDto
{
    public int Id { get; set; }
    public Guid ArticleId { get; set; }
    public int TagId { get; set; }
}