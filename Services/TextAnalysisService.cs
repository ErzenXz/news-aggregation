using Newtonsoft.Json.Linq;

namespace NewsAggregation.Services
{
    public class TextAnalysisService
    {
        public static async Task<string> ExtractTags(string text)
        {
            var client = new HttpClient();
            var response = await client.PostAsync("https://api.meaningcloud.com/topics-2.0", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("key", "YOUR_API_KEY"),
                new KeyValuePair<string, string>("lang", "en"),
                new KeyValuePair<string, string>("txt", text),
                new KeyValuePair<string, string>("tt", "e"),
                new KeyValuePair<string, string>("uw", "y"),
                new KeyValuePair<string, string>("of", "json")
            }));
            var responseContent = await response.Content.ReadAsStringAsync();
            var tags = JObject.Parse(responseContent)["entity_list"].Select(x => x["form"]).ToList();
            return string.Join(", ", tags);
        }
    }
}
