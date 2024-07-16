using NewsAggregation.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace News_aggregation.Entities
{
    public class Article
    {
        public Guid Id { get; set; }
        [ForeignKey("AuthorId")]
        public string AuthorId { get; set; }
        public User Author { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }
        public string Description { get; set; }

        public string ImageUrl { get; set; }
        public string SourceUrl { get; set; }

        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public string Tags { get; set; }
        public int Likes { get; set; }


        public bool IsPublished { get; set; }
        public int Views { get; set; }



        public ICollection<Comment> Comments { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }

    }
}
