namespace NewsAggregation.DTO.Comment;

public class CommentCreateDto
{
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}