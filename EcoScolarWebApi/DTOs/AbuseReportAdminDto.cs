using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using System.Text;

namespace EcoScolarWebApi.DTOs
{
    public class AbuseReportAdminDto
    {
        public int Id { get; set; }
        public long TargetAdvertId { get; set; }
        public int? TargetCommentId { get; set; }
        public string ReporterUserId { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
        public string Message { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReporterNickname { get; set; }
        public string ReporterEmail { get; set; }
        public string SellerNickname { get; set; }
        public string SellerEmail { get; set; }
        public string SellerId { get; set; }
        public string AdvertTitle { get; set; }
        public string AdvertDescription { get; set; }
        public decimal AdvertPrice { get; set; }
        public string? CommentContent { get; set; }
        public string? CommentAnswer { get; set; }
        public string? AuthorNickname { get; set; }
        public string? AuthorEmail { get; set; }
        public string? AuthorId { get; set; }
    }
}
