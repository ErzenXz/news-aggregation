using NewsAggregation.Models;
using System.ComponentModel.DataAnnotations.Schema;
using NewsAggregation.Models.Stats;

namespace News_aggregation.Entities
{
    public class Article
    {
        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public User Author { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }
        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public Guid SourceId { get; set; }
        [ForeignKey("SourceId")]
        public Source Source { get; set; }

        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public string Tags { get; set; }
        public int Likes { get; set; }


        public bool IsPublished { get; set; }
        public int Views { get; set; }



        public ICollection<Comment> Comments { get; set; }
        public ICollection<ArticleStats> ArticleStats { get; set; }


    }
}
