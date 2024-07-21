namespace NewsAggregation.DTO.Favorite;

public class BookmarkCreateDto
{
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
}