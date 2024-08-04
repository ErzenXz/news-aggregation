using AutoMapper;
using News_aggregation.Entities;
using NewsAggregation.DTO.Article;
using NewsAggregation.DTO.ArticleTag;
using NewsAggregation.DTO.Category;
using NewsAggregation.DTO.Comment;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.DTO.UserPreferences;
using NewsAggregation.Models;

namespace NewsAggregation.Helpers;

public class AutoMapperConfiguration : Profile
{
    public AutoMapperConfiguration()
    {
        CreateMap<Article, ArticleDto>().ReverseMap();

        CreateMap<Article, ArticleCreateDto>().ReverseMap();

        CreateMap<Article, ArticleUpdateDto>().ReverseMap();
        CreateMap<UserPreference, UserPreferencesCreateDto>().ReverseMap();
            
        CreateMap<Category, CategoryCreateDto>().ReverseMap();

        CreateMap<Comment, CommentDto>();
        CreateMap<CommentDto, Comment>();

        CreateMap<Comment, CommentCreateDto>().ReverseMap();
        CreateMap<Comment, CommentReportDto>().ReverseMap();

        CreateMap<Bookmark, BookmarkDto>().ReverseMap();
        CreateMap<Bookmark, BookmarkCreateDto>().ReverseMap();
        

    }
}