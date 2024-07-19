using AutoMapper;
using News_aggregation.Entities;
using NewsAggregation.DTO.Article;
using NewsAggregation.DTO.ArticleTag;
using NewsAggregation.DTO.Category;
using NewsAggregation.DTO.Comment;
using NewsAggregation.DTO.Favorite;

namespace NewsAggregation.Helpers;

public class AutoMapperConfiguration : Profile
{
    public AutoMapperConfiguration()
    {
        CreateMap<Article, ArticleDto>().ReverseMap();
        CreateMap<Article, ArticleCreateDto>().ReverseMap();

        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<Category, CategoryCreateDto>().ReverseMap();

        CreateMap<Comment, CommentDto>().ReverseMap();
        CreateMap<Comment, CommentCreateDto>().ReverseMap();

        CreateMap<Bookmark, FavoriteDto>().ReverseMap();
        CreateMap<Bookmark, FavoriteCreateDto>().ReverseMap();




    }
}