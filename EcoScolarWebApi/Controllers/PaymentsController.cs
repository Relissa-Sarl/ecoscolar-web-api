using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
	private readonly IConfiguration _config;        // Configuration to access Stripe secret key
	private readonly EcoscolarDbContext _context;    // Database context

	/// <summary>
	/// PaymentsController constructor
	/// Takes the configuration and database context as parameters
	/// 
	/// Url: POST /api/v1/payments/checkout
	/// </summary>
	/// <param name="config">The configuration object containing the Stripe secret key</param>
	/// <param name="context">The database context</param>
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

		// Handle both single item (for fallback) and list of items
		var productIds = request.ProductIds != null && request.ProductIds.Count > 0
			? request.ProductIds
			: new List<long> { (long)request.ProductId };

		// Check if any product is already sold or currently being paid for (status is SOLD or PAUSED)
		decimal subtotal = 0;
		foreach (var pid in productIds)
		{
			var advert = await _context.Adverts.FindAsync(pid);
			if (advert == null)
			{
				return NotFound(new { error = $"L'annonce avec l'ID {pid} n'existe pas." });
			}
			if (advert.Status == AdvertStatus.PAUSED || advert.Status == AdvertStatus.SOLD)
			{
                return BadRequest(new { code = "ITEM_UNAVAILABLE", error = "Un des articles dans votre panier est en cours de paiement ou déjà vendu." });
            }
			subtotal += advert.Price;
		}

		// Calculate total price with fees and VAT
		decimal shippingCost = request.ShippingMethod == "handToHand" ? 0 : 2;
		decimal serviceFee = subtotal * 0.1m;
		decimal taxTva = (subtotal + shippingCost + serviceFee) * 0.081m;
		decimal total = subtotal + shippingCost + serviceFee + taxTva;
		long priceInCents = (long)System.Math.Round(total * 100, System.MidpointRounding.AwayFromZero);

		// Update all products status to PAUSED during checkout
		foreach (var pid in productIds)
		{
			var advert = await _context.Adverts.FindAsync(pid);
			if (advert != null)
			{
				advert.Status = AdvertStatus.PAUSED;
				_context.Entry(advert).State = EntityState.Modified;
			}
		}
		await _context.SaveChangesAsync();

		string productIdsQuery = string.Join(",", productIds);

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
                        UnitAmount = priceInCents,
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

            SuccessUrl = $"{baseUrl}/success?productIds={productIdsQuery}",
            CancelUrl = $"{baseUrl}/denied?productIds={productIdsQuery}",
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
