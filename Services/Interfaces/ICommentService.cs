using Microsoft.AspNetCore.Mvc;
using News_aggregation.Entities;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;

namespace NewsAggregation.Services.Interfaces;


public interface ICommentService
{
    Task<IActionResult> GetCommentsByArticleId(Guid articleId, string? range = null);
    Task<IActionResult> GetAllComments(string? range = null);
    Task<IActionResult> GetCommentById(Guid id);
    Task<IActionResult> CreateComment(CommentCreateDto comment);
    Task<IActionResult> UpdateComment(Guid id, CommentDto comment);
    Task<IActionResult> DeleteComment(Guid id);
    Task<IActionResult> ReportComment(CommentReportDto commentReportDto);
}