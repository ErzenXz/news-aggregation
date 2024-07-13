using AutoMapper;
using News_aggregation.Entities;
using NewsAggregation.DTO.Article;
using NewsAggregation.DTO.ArticleTag;
using NewsAggregation.DTO.Category;
using NewsAggregation.DTO.Comment;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.DTO.Tag;
using NewsAggregation.DTO.UserPreferences;

namespace NewsAggregation.Helpers;

public class AutoMapperConfiguration : Profile
{
    public AutoMapperConfiguration()
    {
        CreateMap<Article, ArticleDto>().ReverseMap();
        CreateMap<Article, ArticleCreateDto>().ReverseMap();

        CreateMap<ArticleTag, ArticleTagDto>().ReverseMap();
        CreateMap<ArticleTag, ArticleTagCreateDto>().ReverseMap();

        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<Category, CategoryCreateDto>().ReverseMap();

        CreateMap<Comment, CommentDto>().ReverseMap();
        CreateMap<Comment, CommentCreateDto>().ReverseMap();

        CreateMap<Favorite, FavoriteDto>().ReverseMap();
        CreateMap<Favorite, FavoriteCreateDto>().ReverseMap();

        CreateMap<Tag, TagDto>().ReverseMap();
        CreateMap<Tag, TagCreateDto>().ReverseMap();

        CreateMap<UserPreference, UserPreferencesDto>().ReverseMap();
        CreateMap<UserPreference, UserPreferencesCreateDto>().ReverseMap();
    }
}