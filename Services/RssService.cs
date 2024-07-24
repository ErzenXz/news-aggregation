using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using NewsAggregation.DTO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace NewsAggregation.Services
{
    public class RssService
    {
        public static async Task<List<RssArticle>> ParseRssFeed(string rssFeedUrl)
        {
            var items = new List<RssArticle>();

            using (var httpClient = new HttpClient())
            {
                // Set the User-Agent header
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");

                var response = await httpClient.GetAsync(rssFeedUrl);

                // Check if the response is successful
                if (!response.IsSuccessStatusCode)
                {
                    return items;
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var reader = XmlReader.Create(stream))
                    {
                        try
                        {
                            var feed = SyndicationFeed.Load(reader);

                            foreach (var item in feed.Items)
                            {
                                var rssItem = new RssArticle
                                {
                                    Title = item.Title?.Text,
                                    Description = item.Summary?.Text,
                                    Content = item.Content?.ToString(),
                                    Link = item.Links.FirstOrDefault()?.Uri.ToString(),
                                    Image = GetImageFromItem(item),
                                    PubDate = item.PublishDate.DateTime.ToString("dd/MM/yyyy HH:mm")
                                };
                                items.Add(rssItem);
                            }
                        }
                        catch (XmlException ex)
                        {
                            Console.WriteLine($"XML error: {ex.Message}");
                        }
                    }
                }
            }

            return items;
        }

        private static string GetImageFromItem(SyndicationItem item)
        {
            // Check for image in ElementExtensions
            var image = item.ElementExtensions
                .Where(ext => ext.OuterName == "image")
                .Select(ext => ext.GetObject<XElement>()?.Value)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(image))
            {
                return image;
            }

            // Check for media:content
            var mediaContent = item.ElementExtensions
                .Where(ext => ext.OuterName == "content" && ext.OuterNamespace == "http://search.yahoo.com/mrss/")
                .Select(ext => ext.GetObject<XElement>()?.Attribute("url")?.Value)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(mediaContent))
            {
                return mediaContent;
            }

            // Check for media:thumbnail
            var mediaThumbnail = item.ElementExtensions
                .Where(ext => ext.OuterName == "thumbnail" && ext.OuterNamespace == "http://search.yahoo.com/mrss/")
                .Select(ext => ext.GetObject<XElement>()?.Attribute("url")?.Value)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(mediaThumbnail))
            {
                return mediaThumbnail;
            }

            // Check for image in description (as a fallback)
            var description = item.Summary?.Text;
            if (!string.IsNullOrEmpty(description))
            {
                var match = Regex.Match(description, "<img.+?src=\"'[\"'].*?>", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
    }
}
