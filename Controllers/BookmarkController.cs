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
    public async Task<IActionResult> GetAllBookmarks()
    {
        var bookmarks = await _bookmarkService.GetAllBookmarks();
        return Ok(bookmarks);
    }

    [HttpGet("GetBookmarksList")]
    public async Task<IActionResult> GetBookmarksList(string searchByUser, int page, int pageSize, Guid articleId)
    {
        var bookmarksList = await _bookmarkService.BookmarksListView(searchByUser, page, pageSize, articleId);
        return Ok(bookmarksList);
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