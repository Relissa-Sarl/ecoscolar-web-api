using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class AbuseReportMapper
{
    public partial AbuseReportResponseDto ToAbuseReportResponse(AbuseReport report);

    public partial IQueryable<AbuseReportResponseDto> ProjectToAbuseReportResponses(IQueryable<AbuseReport> query);
}
