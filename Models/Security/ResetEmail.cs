using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NewsAggregation.Models.Security
{
    public class ResetEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ValidUntil { get; set;}
    }
}
