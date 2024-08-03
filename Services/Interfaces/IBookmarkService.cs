using Microsoft.AspNetCore.Mvc;
using News_aggregation.Entities;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Helpers;
using NewsAggregation.Models;

namespace NewsAggregation.Services.Interfaces;

public interface IBookmarkService
{ 
    Task<IActionResult> GetAllBookmarks(string? range = null);
    Task<IActionResult> GetBookmarksByArticleId(Guid articleId, string? range = null);
    Task<IActionResult> GetBookmarkById(Guid id);
    Task<IActionResult> CreateBookmark(BookmarkCreateDto bookmark);
    Task<IActionResult> DeleteBookmark(Guid id);
    Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent);
}