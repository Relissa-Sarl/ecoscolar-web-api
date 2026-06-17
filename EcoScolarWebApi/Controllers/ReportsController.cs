using Asp.Versioning;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ReportsController(IAbuseReportService reportService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AbuseReportResponseDto>> CreateReport([FromBody] AbuseReportRequestDto requestDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var response = await reportService.CreateReportAsync(requestDto, userId);
        return CreatedAtAction(nameof(CreateReport), new { id = response.Id }, response);
    }
}
