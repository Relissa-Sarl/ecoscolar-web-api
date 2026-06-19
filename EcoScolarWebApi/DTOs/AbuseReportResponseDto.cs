using EcoScolarWebApi.Enums;

namespace EcoScolarWebApi.DTOs;

public class AbuseReportResponseDto
{
    public int Id { get; set; }
    public long TargetAdvertId { get; set; }
    public int? TargetCommentId { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public ReportReason Reason { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
