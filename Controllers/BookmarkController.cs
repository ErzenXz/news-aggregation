using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers;

[Route("bookmark")]
[ApiController]
public class BookmarkController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarkController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet("all"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAllBookmarks(string? range = null)
    {
        var bookmarks = await _bookmarkService.GetAllBookmarks(range);
        return Ok(bookmarks);
    }

    [HttpGet("article/{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetBookmarksByArticleId(Guid id, string? range = null)
    {
        var bookmarks = await _bookmarkService.GetBookmarksByArticleId(id,range);
        return Ok(bookmarks);
    }

    [HttpGet("{id}|"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetBookmarkById(Guid id)
    {
        var bookmark = await _bookmarkService.GetBookmarkById(id);
        return Ok(bookmark);
    }

    [HttpPost("create"), Authorize(Roles = "User,Admin,SuperAdmin")]
    public async Task<IActionResult> CreateBookmark([FromBody]BookmarkCreateDto bookmark)
    {
        await _bookmarkService.CreateBookmark(bookmark);
        return Ok();
    }

    [HttpDelete("{id}"), Authorize(Roles = "User,Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        await _bookmarkService.DeleteBookmark(id);
        return Ok();
    }
}