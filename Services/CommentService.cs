using AutoMapper;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<Comment>> GetAllComments()
    {
        var comments = await _unitOfWork.Repository<Comment>().GetAll().ToListAsync();
        return comments;
    }

    public async Task<PagedInfo<CommentDto>> CommentsListView(string searchByUser, int page, int pageSize, Guid articleId)
    {
        IQueryable<Comment> comments;

        if (articleId != null)
        {
            comments = _unitOfWork.Repository<Comment>().GetByCondition(x => x.ArticleId == articleId);
        }
        else
        {
            comments = _unitOfWork.Repository<Comment>().GetAll();
        }

        comments = comments.WhereIf(!string.IsNullOrEmpty(searchByUser), x => x.User.FullName.Contains(searchByUser));
        
        var commentsList = await comments.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var mappedComments = _mapper.Map<List<CommentDto>>(commentsList);

        var pagedComments = new PagedInfo<CommentDto>()
        {
            TotalCount = await comments.CountAsync(),
            Page = page,
            PageSize = pageSize,
            Items = mappedComments
        };

        return pagedComments;
    }

    public async Task<Comment> GetCommentById(Guid id)
    {
        var comment = _unitOfWork.Repository<Comment>().GetByCondition(x => x.Id == id).FirstOrDefaultAsync();
        return await comment;
    }

    public async Task CreateComment(CommentCreateDto comment)
    {
        var commentToCreate = _mapper.Map<Comment>(comment);
        commentToCreate.Id = Guid.NewGuid();
        _unitOfWork.Repository<Comment>().Create(commentToCreate);
        _unitOfWork.Complete();
    }

    public async Task UpdateComment(CommentDto comment)
    {
        var commentToUpdate =
            await _unitOfWork.Repository<Comment>().GetByCondition(x => x.Id == comment.Id).FirstOrDefaultAsync();

        if (comment != null)
        {
            commentToUpdate.UpdatedAt = DateTime.Now;
            commentToUpdate.Content = comment.Content;
        }
        _unitOfWork.Repository<Comment>().Update(commentToUpdate);
        _unitOfWork.Complete();

    }

    public async Task DeleteComment(Guid id)
    {
        var commentToDelete =
            await _unitOfWork.Repository<Comment>().GetByCondition(x => x.Id == id).FirstOrDefaultAsync();
        
        _unitOfWork.Repository<Comment>().Delete(commentToDelete);
        _unitOfWork.Complete();
    }
}