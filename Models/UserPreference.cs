using System.ComponentModel.DataAnnotations.Schema;
using Amazon.S3.Model;
using News_aggregation.Entities;

namespace NewsAggregation.Models
{
    public class UserPreference
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

    }
}
