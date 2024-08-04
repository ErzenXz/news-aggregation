using static NewsAggregation.Helpers.CommentReportTypes;

namespace NewsAggregation.Models
{
    public class CommentReports
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid CommentId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public CommentReportType ReportType { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Status { get; set; }
        public bool? IsSeen { get; set; }
    }
}
