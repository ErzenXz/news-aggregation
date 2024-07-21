using News_aggregation.Entities;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Helpers;

namespace NewsAggregation.Services.Interfaces;

public interface IBookmarkService
{
    Task<List<Bookmark>> GetAllBookmarks();
    Task<PagedInfo<BookmarkDto>> BookmarksListView(string searchByUser, int page, int pageSize, Guid articleId);
    Task<Bookmark> GetBookmarkById(Guid id);
    Task CreateBookmark(BookmarkCreateDto bookmark);
    Task DeleteBookmark(Guid id);
}