namespace NewsAggregation.DTO.Favorite;

public class BookmarkDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
}