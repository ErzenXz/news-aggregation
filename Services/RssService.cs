using System.ServiceModel.Syndication;
using System.Xml;
using NewsAggregation.DTO;

namespace NewsAggregation.Services
{
    public class RssService
    {
 

    public List<RssArticle> ParseRssFeed(string rssFeedUrl)
    {

        using var reader = XmlReader.Create(rssFeedUrl);
        var feed = SyndicationFeed.Load(reader);
        var items = new List<RssArticle>();

        foreach (var item in feed.Items)
        {
            var rssItem = new RssArticle
            { 
                Title = item.Title.Text,
                Description = item.Summary.Text,
                Link = item.Links.FirstOrDefault()?.Uri.ToString(),
                Image = item.ElementExtensions
                    .Where(ext => ext.OuterName == "image")
                    .Select(ext => ext.GetObject<Uri>().ToString())
                    .FirstOrDefault(),
                PubDate = item.PublishDate.DateTime.ToString("dd/MM/yyyy HH:mm")
                
            };
            items.Add(rssItem);
        }

        return items;
    }
    }
}
