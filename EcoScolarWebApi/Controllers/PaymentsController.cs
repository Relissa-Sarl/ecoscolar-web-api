using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IConfiguration config,
        IPaymentService paymentService,
        UserManager<User> userManager,
        ILogger<PaymentsController> logger)
    {
        _config = config;
        _paymentService = paymentService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe Checkout session and returns its URL. The amount and platform fee
    /// are computed server-side from the adverts; the client never sends a price.
    ///
    /// Url: POST /api/v1/payments/checkout
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
    {
        var buyerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(buyerId))
            return Unauthorized();

        var baseUrl = ResolveFrontendBaseUrl();

        var result = await _paymentService.CreateCheckoutSessionAsync(request, buyerId, baseUrl);
        if (result.IsSuccess)
            return Ok(new { url = result.Data!.Url, orderNumber = result.Data.OrderNumber });

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { result.Errors }),
            ErrorType.Conflict => Conflict(new { code = "ITEM_UNAVAILABLE", result.Errors }),
            ErrorType.InternalError => StatusCode(StatusCodes.Status502BadGateway, new { result.Errors }),
            _ => BadRequest(new { result.Errors }),
        };
    }

    /// <summary>
    /// Verifies the Stripe event signature, then applies the checkout session result to the
    /// matching transactions. Idempotent (Stripe replays events).
    ///
    /// Url: POST /api/v1/payments/webhook
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var webhookSecret = _config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("Stripe:WebhookSecret is not configured; rejecting webhook.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false — the connected Stripe account may run a newer API
            // version than the one pinned in Stripe.net. We only read stable fields (session id,
            // payment intent id), so a version skew is safe and must not reject the event.
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException e)
        {
            _logger.LogWarning(e, "Invalid Stripe webhook signature.");
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            {
                var session = (Session)stripeEvent.Data.Object;
                await _paymentService.ConfirmCheckoutSessionAsync(session.Id, session.PaymentIntentId);
                break;
            }
            case "checkout.session.expired":
            case "checkout.session.async_payment_failed":
            {
                var session = (Session)stripeEvent.Data.Object;
                await _paymentService.CancelCheckoutSessionAsync(session.Id);
                break;
            }
            default:
                _logger.LogInformation("Unhandled Stripe event type {EventType}.", stripeEvent.Type);
                break;
        }

        // Always acknowledge handled events so Stripe stops retrying.
        return Ok();
    }

    /// <summary>
    /// Gets a Stripe Checkout session by its ID.
    ///
    /// Url: GET /api/v1/payments/session/{sessionId}
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        try
        {
            var session = await new SessionService().GetAsync(sessionId);
            return Ok(new { amountTotal = session.AmountTotal });
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
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
