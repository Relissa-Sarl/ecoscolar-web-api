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
[Route("api/v{version:apiVersion}/tickets")]
[ApiController]
public class TicketsController(ISupportContactService supportContactService) : ControllerBase
{
    /// <summary>
    /// Creates a support ticket. POST /api/v1/tickets
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
    /// Lists support tickets for the authenticated user. GET /api/v1/tickets
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        var result = await supportContactService.GetMyTicketsAsync(User);

        if (result.IsSuccess)
            return Ok(result.Data);

        return MapError(result);
    }

    /// <summary>
    /// Gets one support ticket. GET /api/v1/tickets/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetMyTicket(int id)
    {
        var result = await supportContactService.GetMyTicketAsync(User, id);

        if (result.IsSuccess)
            return Ok(result.Data);

        return MapError(result);
    }

    /// <summary>
    /// Lists conversation messages. GET /api/v1/tickets/{id}/messages
    /// </summary>
    [HttpGet("{id:int}/messages")]
    [Authorize]
    public async Task<IActionResult> GetTicketMessages(int id)
    {
        var result = await supportContactService.GetTicketMessagesAsync(User, id);

        if (result.IsSuccess)
            return Ok(result.Data);

        return MapError(result);
    }

    /// <summary>
    /// Adds a user reply to a ticket. POST /api/v1/tickets/{id}/messages
    /// </summary>
    [HttpPost("{id:int}/messages")]
    [Authorize]
    public async Task<IActionResult> AddTicketMessage(int id, [FromBody] SupportTicketMessageRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await supportContactService.AddTicketMessageAsync(User, id, request);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Data);

        return MapError(result);
    }

    private IActionResult MapError<T>(Result<T> result) =>
        result.ErrorType switch
        {
            ErrorType.Unauthorized => Unauthorized(new { result.Errors }),
            ErrorType.NotFound => NotFound(new { result.Errors }),
            ErrorType.Conflict => BadRequest(new { result.Errors }),
            _ => BadRequest(new { result.Errors })
        };
}
