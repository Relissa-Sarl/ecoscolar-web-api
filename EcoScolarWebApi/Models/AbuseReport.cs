using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoScolarWebApi.Enums;

namespace EcoScolarWebApi.Models;

[Table("AbuseReports")]
public class AbuseReport
{
    [Key]
    public int Id { get; set; }

    [Required]
    public long TargetAdvertId { get; set; }

    public int? TargetCommentId { get; set; }

    [Required]
    public string ReporterUserId { get; set; } = string.Empty;

    [Required]
    public ReportReason Reason { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(TargetAdvertId))]
    public virtual Advert? TargetAdvert { get; set; }

    [ForeignKey(nameof(TargetCommentId))]
    public virtual PublicComment? TargetComment { get; set; }

    [ForeignKey(nameof(ReporterUserId))]
    public virtual User? Reporter { get; set; }
}
