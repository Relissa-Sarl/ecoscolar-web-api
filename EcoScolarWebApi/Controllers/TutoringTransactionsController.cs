using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Tutoring;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tutoring/transactions")]
[ApiController]
[Authorize]
public class TutoringTransactionsController(
    ITutoringTransactionService tutoringTransactionService,
    UserManager<User> userManager) : ControllerBase
{
    private readonly ITutoringTransactionService _tutoringTransactionService = tutoringTransactionService;
    private readonly UserManager<User> _userManager = userManager;

    [HttpPatch("{transactionId}/accept")]
    public async Task<IActionResult> Accept(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return ToActionResult(await _tutoringTransactionService.AcceptAsync(transactionId, user.Id));
    }

    [HttpPatch("{transactionId}/refuse")]
    public async Task<IActionResult> Refuse(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return ToActionResult(await _tutoringTransactionService.RefuseAsync(transactionId, user.Id));
    }

    [HttpPatch("{transactionId}/confirm")]
    public async Task<IActionResult> Confirm(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return ToActionResult(await _tutoringTransactionService.ConfirmAsync(transactionId, user.Id));
    }

    [HttpPatch("{transactionId}/mark-rendered")]
    public async Task<IActionResult> MarkRendered(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return ToActionResult(await _tutoringTransactionService.MarkRenderedAsync(transactionId, user.Id));
    }

    [HttpGet("{transactionId}/tutor-contact")]
    [ProducesResponseType(typeof(TutorContactDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTutorContact(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var result = await _tutoringTransactionService.GetTutorContactAsync(transactionId, user.Id);
        if (!result.IsSuccess)
            return ToActionResult(result);

        return Ok(result.Data);
    }

    private IActionResult ToActionResult(Result result) =>
        result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
            ErrorType.Forbidden => Forbid(),
            ErrorType.BadRequest => BadRequest(new { message = result.Errors.FirstOrDefault() }),
            _ => result.IsSuccess ? Ok() : StatusCode(500, new { message = result.Errors.FirstOrDefault() })
        };

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
            ErrorType.Forbidden => Forbid(),
            ErrorType.BadRequest => BadRequest(new { message = result.Errors.FirstOrDefault() }),
            _ => result.IsSuccess ? Ok(result.Data) : StatusCode(500, new { message = result.Errors.FirstOrDefault() })
        };
}
