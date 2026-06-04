using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoScolarWebApi.Controllers;

/// <summary>
/// Contact support / send feedback.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class SupportController(ISupportContactService supportContactService) : ControllerBase
{
    /// <summary>
    /// Creates a support ticket. POST /api/v1/support
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] SupportContactRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await supportContactService.SubmitAsync(request, User);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Data);

        return BadRequest(new { result.Errors });
    }

    /// <summary>
    /// Lists support tickets for the authenticated user. GET /api/v1/support/mine
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        var result = await supportContactService.GetMyTicketsAsync(User);

        if (result.IsSuccess)
            return Ok(result.Data);

        return result.ErrorType switch
        {
            ErrorType.Unauthorized => Unauthorized(new { result.Errors }),
            ErrorType.NotFound => NotFound(new { result.Errors }),
            _ => BadRequest(new { result.Errors })
        };
    }
}