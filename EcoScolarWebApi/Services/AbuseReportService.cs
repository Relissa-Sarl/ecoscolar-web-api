using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.Services;

public class AbuseReportService(EcoscolarDbContext context) : IAbuseReportService
{
    public async Task<AbuseReportResponseDto> CreateReportAsync(AbuseReportRequestDto requestDto, string reporterUserId)
    {
        var report = new AbuseReport
        {
            TargetAdvertId = requestDto.TargetAdvertId,
            TargetCommentId = requestDto.TargetCommentId,
            Reason = requestDto.Reason,
            Message = requestDto.Message,
            ReporterUserId = reporterUserId,
            Status = TicketStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        context.AbuseReports.Add(report);
        await context.SaveChangesAsync();

        var mapper = new AbuseReportMapper();
        return mapper.ToAbuseReportResponse(report);
    }
}
