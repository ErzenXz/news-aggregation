using News_aggregation.Entities;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;

namespace NewsAggregation.Services.Interfaces;

public interface ICommentService
{
    Task<List<Comment>> GetAllComments();
    Task<PagedInfo<CommentDto>> CommentsListView(string searchByUser, int page, int pageSize, Guid articleId);
    Task<Comment> GetCommentById(Guid id);
    Task CreateComment(CommentCreateDto comment);
    Task UpdateComment(CommentDto comment);
    Task DeleteComment(Guid id);
}