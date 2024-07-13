namespace NewsAggregation.DTO.Comment;

public class CommentCreateDto
{
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}