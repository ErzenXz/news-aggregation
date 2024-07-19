using System.ComponentModel.DataAnnotations.Schema;
using Amazon.S3.Model;
using News_aggregation.Entities;

namespace NewsAggregation.Models
{
    public class UserPreference
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public string Tags { get; set; }

    }
}
