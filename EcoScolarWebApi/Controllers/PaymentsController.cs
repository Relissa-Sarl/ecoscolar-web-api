using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Stripe;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
	private readonly IConfiguration _config;        // Configuration to access Stripe secret key
	private readonly EcoscolarDbContext _context;

	/// <summary>
	/// PaymentsController constructor
	/// Takes the configuration as a parameter to access the Stripe secret key
	/// 
	/// Url: POST /api/v1/payments/checkout
	/// </summary>
	/// <param name="config">The configuration object containing the Stripe secret key</param>
	public PaymentsController(IConfiguration config, EcoscolarDbContext context)
	{
		_config = config;
		_context = context;
	}

	/// <summary>
	/// Creates a Stripe Checkout session for a given product price 
	/// and returns the session URL to the client
	/// 
	/// Url: POST /api/v1/payments/checkout
	/// </summary>
	/// <param name="request">The checkout request containing product information</param>
	/// <returns>The session URL</returns>
	[HttpPost("checkout")]
	public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
	{
		var advert = await _context.Adverts.FindAsync(request.AdvertId);
		if (advert == null) return NotFound(new { error = "Advert not found" });

		double price = (double)advert.Price * 100;
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        if (Request.Headers.TryGetValue("Referer", out var refererHeader) && !string.IsNullOrEmpty(refererHeader))
        {
            try
            {
                var uri = new Uri(refererHeader.ToString());
                baseUrl = $"{uri.Scheme}://{uri.Authority}";
            }
            catch
            {
                // Fallback in case of malformed Referer
            }
        }

        var options = new SessionCreateOptions
		{
			PaymentMethodTypes = new List<string> { "card" },
			LineItems = new List<SessionLineItemOptions>
			{
				new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
                        // 1 franc = 100 cents
                        UnitAmount = (long)price,
						Currency = "chf",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = "Amount due",
							Description = "Thank you for choosing EcoScolar for your school supplies. Good luck with your studies!"
                        },
					},
					Quantity = 1,
				},
			},
			Mode = "payment",
			PaymentIntentData = new SessionPaymentIntentDataOptions
			{
				TransferGroup = "COMMANDE_ID_789",
			},

            SuccessUrl = $"{baseUrl}/success?orderId={{CHECKOUT_SESSION_ID}}&productId={request.ProductId}",
            CancelUrl = $"{baseUrl}/denied",
		};

		var service = new SessionService();
		Session session = await service.CreateAsync(options);

		return Ok(new { url = session.Url });
	}

	/// <summary>
	/// Creates a transfer to a connected account using the Stripe API
	/// 
	/// Url: POST /api/v1/payments/create-transfer
	/// </summary>
	/// <param name="request">The transfer request containing transfer information</param>
	/// <returns>The transfer ID</returns>
	[HttpPost("create-transfer")]
	public async Task<IActionResult> CreateTransfer([FromBody] TransferRequestDto request)
	{
		try
		{
			var options = new TransferCreateOptions
			{
				Amount = request.Amount,
				Currency = "chf",
				Destination = request.ConnectedAccountId,
				TransferGroup = request.TransferGroup,
			};

			var transferService = new TransferService();
			Transfer transfer = await transferService.CreateAsync(options);

			return Ok(new { transferId = transfer.Id });
		}
		catch (StripeException e)
		{
			return BadRequest(new { error = e.StripeError.Message });
		}
	}
}
