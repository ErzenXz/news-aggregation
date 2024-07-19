namespace NewsAggregation.DTO.Favorite;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
}