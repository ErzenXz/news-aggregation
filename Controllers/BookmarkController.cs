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

    [HttpGet("GetAllBookmarks")]
    public async Task<IActionResult> GetAllBookmarks(string? range = null)
    {
        var bookmarks = await _bookmarkService.GetAllBookmarks(range);
        return Ok(bookmarks);
    }

    [HttpGet("GetBookmarksByArticleId/{id}")]
    public async Task<IActionResult> GetBookmarksByArticleId(Guid id)
    {
        var bookmarks = await _bookmarkService.GetBookmarksByArticleId(id);
        return Ok(bookmarks);
    }

    [HttpGet("GetBookmarkById/{id}|")]
    public async Task<IActionResult> GetBookmarkById(Guid id)
    {
        var bookmark = _bookmarkService.GetBookmarkById(id);
        return Ok(bookmark);
    }

    [HttpPost("CreateBookmark")]
    public async Task<IActionResult> CreateBookmark([FromBody]BookmarkCreateDto bookmark)
    {
        await _bookmarkService.CreateBookmark(bookmark);
        return Ok();
    }

    [HttpDelete("DeleteBookmark/{id}")]
    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        await _bookmarkService.DeleteBookmark(id);
        return Ok();
    }
}