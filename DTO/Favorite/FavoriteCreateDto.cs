namespace NewsAggregation.DTO.Favorite;

public class FavoriteCreateDto
{
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
}