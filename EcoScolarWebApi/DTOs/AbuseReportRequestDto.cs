using EcoScolarWebApi.Enums;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs;

public class AbuseReportRequestDto
{
    [Required]
    public long TargetAdvertId { get; set; }

    public int? TargetCommentId { get; set; }

    [Required]
    public ReportReason Reason { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Message { get; set; } = string.Empty;
}
