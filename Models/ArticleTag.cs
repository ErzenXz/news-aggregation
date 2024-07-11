namespace News_aggregation.Entities
{
    public class ArticleTag
    {
        public int Id { get; set; }
        public Guid ArticleId { get; set; }
        public Article Article { get; set; }
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
