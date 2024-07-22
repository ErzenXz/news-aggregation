using News_aggregation.Entities;
using NewsAggregation.DTO.Article;
using NewsAggregation.Models;

namespace NewsAggregation.Helpers
{
    public class ArticlesAndAds
    {
        public List<Article> Articles { get; set; }
        public List<Ads> Ads { get; set; }
    }
}
