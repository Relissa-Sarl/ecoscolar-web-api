using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Tutoring;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class TutoringController(ITutoringReservationService reservationService, UserManager<User> userManager) : ControllerBase
{
	/// <summary>
	/// Reserves a tutoring package (a number of hours) and returns a Stripe Checkout session URL.
	/// The price is computed server-side from the advert's hourly rate; the client only sends the hours.
	///
	/// Url: POST /api/v1/tutoring/{advertId}/reserve
	/// </summary>
	[HttpPost("{advertId}/reserve")]
    public async Task<IActionResult> Reserve(long advertId, [FromBody] TutoringReserveRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var buyerId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(buyerId))
            return Unauthorized();

        var baseUrl = ResolveFrontendBaseUrl();

        var result = await reservationService.CreateReservationSessionAsync(advertId, request.Hours, buyerId, baseUrl);
        if (result.IsSuccess)
            return Ok(new { url = result.Data!.Url, orderNumber = result.Data.OrderNumber });

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { result.Errors }),
            ErrorType.Conflict => Conflict(new { result.Errors }),
            ErrorType.InternalError => StatusCode(StatusCodes.Status502BadGateway, new { result.Errors }),
            _ => BadRequest(new { result.Errors }),
        };
    }

    /// <summary>
    /// Resolves the frontend base URL from the Referer header, falling back to the request host.
    /// </summary>
    private string ResolveFrontendBaseUrl()
    {
        if (Request.Headers.TryGetValue("Referer", out var refererHeader) && !string.IsNullOrEmpty(refererHeader))
        {
            try
            {
                var uri = new Uri(refererHeader.ToString());
                return $"{uri.Scheme}://{uri.Authority}";
            }
            catch
            {
                // Fallback below in case of a malformed Referer.
            }
        }

        return $"{Request.Scheme}://{Request.Host}";
    }
}
