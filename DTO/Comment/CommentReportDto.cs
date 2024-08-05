using static NewsAggregation.Helpers.CommentReportTypes;

namespace NewsAggregation.DTO.Comment
{
    public class CommentReportDto
    {
        public Guid CommentId { get; set; }
        public CommentReportType ReportType { get; set; }
    }
}
