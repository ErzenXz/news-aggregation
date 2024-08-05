using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers;

[Route("comment")]
[ApiController]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommentById(Guid id)
    {
        var comment = await _commentService.GetCommentById(id);
        return Ok(comment);
    }

    [HttpGet("article/{articleId}")]
    public async Task<IActionResult> GetCommentsByArticleId(Guid articleId, string? range = null)
    {
        var comments = await _commentService.GetCommentsByArticleId(articleId, range);
        return Ok(comments);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetComments(string? range = null)
    {
        var comments = await _commentService.GetAllComments(range);
        return Ok(comments);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateComment([FromBody] CommentCreateDto comment)
    {
        await _commentService.CreateComment(comment);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] CommentDto comment)
    {
        await _commentService.UpdateComment(id, comment);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        await _commentService.DeleteComment(id);
        return Ok();
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportComment(CommentReportDto commentReport)
    {
        await _commentService.ReportComment(commentReport);
        return Ok();
    }

    [HttpGet("getReported")]
    public async Task<IActionResult> GetAllReportedComments()
    {
        var reportedComments = await _commentService.GetAllReportedComments();
        return Ok(reportedComments);
    }

    [HttpPut("updateReported/{id}")]
    public async Task<IActionResult> UpdateReportComment(Guid id, [FromBody] sbyte status)
    {
        await _commentService.UpdateReportComment(id, status);
        return Ok();
    }
}