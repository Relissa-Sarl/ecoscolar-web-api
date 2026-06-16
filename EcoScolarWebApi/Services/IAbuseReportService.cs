using EcoScolarWebApi.DTOs;

namespace EcoScolarWebApi.Services;

public interface IAbuseReportService
{
    Task<AbuseReportResponseDto> CreateReportAsync(AbuseReportRequestDto requestDto, string reporterUserId);
}
