using AutoMapper;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services;

public class BookmarkService : IBookmarkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookmarkService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }


    public async Task<List<Bookmark>> GetAllBookmarks()
    {
        var bookmarks = await _unitOfWork.Repository<Bookmark>().GetAll().ToListAsync();
        return bookmarks;
    }

    public async Task<PagedInfo<BookmarkDto>> BookmarksListView(string searchByUser, int page, int pageSize, Guid articleId)
    {
        IQueryable<Bookmark> bookmarks;
        
        if (articleId != null)
        {
            bookmarks = _unitOfWork.Repository<Bookmark>().GetByCondition(x => x.Article.Id == articleId);
        }
        else
        {
            bookmarks = _unitOfWork.Repository<Bookmark>().GetAll();
        }

        bookmarks = bookmarks.WhereIf(!string.IsNullOrEmpty(searchByUser), x => x.User.FullName.Contains(searchByUser));

        var bookmarksList = bookmarks.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var mappedBookmarks = _mapper.Map<List<BookmarkDto>>(bookmarksList);

        var pagedBookmarks = new PagedInfo<BookmarkDto>()
        {
            TotalCount = await bookmarks.CountAsync(),
            Page = page,
            PageSize = pageSize,
            Items = mappedBookmarks
        };

        return pagedBookmarks;
    }

    public async Task<Bookmark> GetBookmarkById(Guid id)
    {
        var bookmark = await _unitOfWork.Repository<Bookmark>().GetByCondition(x => x.Id == id).FirstOrDefaultAsync();
        return bookmark;
    }

    public async Task CreateBookmark(BookmarkCreateDto bookmark)
    {
        var bookmarkToCreate = _mapper.Map<Bookmark>(bookmark);
        bookmarkToCreate.Id = Guid.NewGuid();
        _unitOfWork.Repository<Bookmark>().Create(bookmarkToCreate);
        _unitOfWork.Complete();
    }

    public async Task DeleteBookmark(Guid id)
    {
        var bookmarkToDelete =
            await _unitOfWork.Repository<Bookmark>().GetByCondition(x => x.Id == id).FirstOrDefaultAsync();
        
        _unitOfWork.Repository<Bookmark>().Delete(bookmarkToDelete);
        _unitOfWork.Complete();
    }
}