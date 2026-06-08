using Asp.Versioning;
using EcoScolarWebApi.DTOs.Stripe;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.V2.Core;
using EcoScolarWebApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
	private readonly IConfiguration _config;
	private readonly EcoscolarDbContext _context;

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
	[Microsoft.AspNetCore.Authorization.Authorize]
	public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
	{
		var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId))
			return Unauthorized();

		var advert = await _context.Adverts.FindAsync(request.AdvertId);
		if (advert == null || advert.Status != Enums.AdvertStatus.ACTIVE)
			return NotFound(new { error = "L'annonce n'existe pas ou n'est plus disponible." });

		if (advert.ReservedUntil > DateTime.UtcNow && advert.ReservedByUserId != userId)
			return BadRequest(new { error = "L'annonce est réservée par un autre utilisateur." });

		// Mettre à jour la réservation
		advert.ReservedUntil = DateTime.UtcNow.AddMinutes(30); // Donne 30 min pour payer
		advert.ReservedByUserId = userId;

		// Créer la transaction PENDING
		decimal shippingCost = 7.00m;
		var transaction = new EcoScolarWebApi.Models.Transaction
		{
			AdvertId = advert.AdvertId,
			BuyerId = userId,
			Date = DateTime.UtcNow,
			Status = Enums.TransactionStatus.PENDING_PAYMENT,
			ShippingAddress = request.ShippingAddress,
			ShippingCost = shippingCost,
			PlatformFee = advert.Price * 0.10m // Exemple de frais de plateforme (10%)
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		double priceTotalCents = (double)(advert.Price + shippingCost) * 100;

		var options = new SessionCreateOptions
		{
			PaymentMethodTypes = new List<string> { "card" },
			ClientReferenceId = transaction.TransactionId.ToString(), // Pour le webhook
			LineItems = new List<SessionLineItemOptions>
			{
				new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)priceTotalCents,
						Currency = "chf",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = advert.Title,
							Description = $"Achat de {advert.Title} incluant {shippingCost} CHF de frais de port."
						},
					},
					Quantity = 1,
				},
			},
			Mode = "payment",
			// PaymentIntentData pour lier au groupe de transfert (si on fait des transferts séparés)
			PaymentIntentData = new SessionPaymentIntentDataOptions
			{
				TransferGroup = $"TRANS_{transaction.TransactionId}",
			},
			SuccessUrl = "http://localhost:3000/success?orderId={CHECKOUT_SESSION_ID}",
			CancelUrl = "http://localhost:3000/cart",
		};

		var service = new SessionService();
		Session session = await service.CreateAsync(options);

		// Sauvegarder l'ID de session Stripe
		transaction.StripeSessionId = session.Id;
		await _context.SaveChangesAsync();

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

	/// <summary>
	/// Creates a Stripe Connect account for an individual in Switzerland using the Stripe API v2
	///     
	/// Url: POST /api/v1/payments/create-connect-account
	/// </summary>
	/// <param name="request">The request containing account information</param>
	/// <returns>The created account ID</returns>
	[HttpPost("create-connect-account")]
	public IActionResult CreateConnectAccount([FromBody] ConnectAccountRequestDto request)
	{
		// Basic validation
		if (string.IsNullOrWhiteSpace(request?.Email))
		{
			Console.WriteLine("Email is required");
			return BadRequest(new { error = "Email is required." });
		}

		try
		{
			// Create v2 Connect account for an individual in Switzerland
			var options = new Stripe.V2.Core.AccountCreateOptions
			{
				ContactEmail = request.Email,
				DisplayName = request.Email,
				Identity = new AccountCreateIdentityOptions
				{
					Country = "CH", // Changed to Switzerland
					EntityType = "individual", // Changed to individual (particular)
				},
				Configuration = new AccountCreateConfigurationOptions
				{
					Recipient = new AccountCreateConfigurationRecipientOptions
					{
						Capabilities = new AccountCreateConfigurationRecipientCapabilitiesOptions
						{
							StripeBalance = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceOptions
							{
								StripeTransfers = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceStripeTransfersOptions
								{
									Requested = true,
								},
							},
						},
					},
				},
				Defaults = new AccountCreateDefaultsOptions
				{
					Responsibilities = new AccountCreateDefaultsResponsibilitiesOptions
					{
						FeesCollector = "application",
						LossesCollector = "application",
					},
				},
				Dashboard = "express",
				Include = new List<string>
				{
					"configuration.recipient",
					"requirements",
				},
			};

			var secretKey = _config["Stripe:SecretKey"];
			var client = new StripeClient(secretKey);

			var service = client.V2.Core.Accounts;
			Stripe.V2.Core.Account account = service.Create(options);

			return Ok(new { accountId = account.Id });
		}
		catch (StripeException e)
		{
			// Catching StripeException specifically can be useful for debugging
			return StatusCode(500, new { error = e.StripeError.Message });
		}
		catch (Exception e)
		{
			return StatusCode(500, new { error = e.Message });
		}
	}

	/// <summary>
	/// Creates an account link for onboarding a connected account using the Stripe API v2
	/// 
	/// Url: POST /api/v1/payments/create-account-link
	/// </summary>
	/// <param name="request">The request containing account link information</param>
	/// <returns>The created account link URL</returns>
	[HttpPost("create-account-link")]
	public IActionResult CreateAccountLink([FromBody] AccountLinkRequestDto request)
	{
		// Basic validation
		if (string.IsNullOrWhiteSpace(request?.AccountId))
		{
			return BadRequest(new { error = "Account ID is required." });
		}

		try
		{
			var secretKey = _config["Stripe:SecretKey"];
			var client = new StripeClient(secretKey);
			var service = client.V2.Core.AccountLinks;

			var options = new Stripe.V2.Core.AccountLinkCreateOptions
			{
				Account = request.AccountId,
				UseCase = new Stripe.V2.Core.AccountLinkCreateUseCaseOptions
				{
					Type = "account_onboarding",
					AccountOnboarding = new Stripe.V2.Core.AccountLinkCreateUseCaseAccountOnboardingOptions
					{
						Configurations = new List<string> { "recipient" },
						// Note: You should replace these example URLs with your actual front-end URLs
						RefreshUrl = "http://localhost:3001/home",
						ReturnUrl = $"http://localhost:3001/home?accountId={request.AccountId}",
					},
				},
			};

			var accountLink = service.Create(options);

			return Ok(new { url = accountLink.Url });
		}
		catch (StripeException e)
		{
			// Catching StripeException specifically can be useful for debugging
			return StatusCode(500, new { error = e.StripeError.Message });
		}
		catch (Exception e)
		{
			return StatusCode(500, new { error = e.Message });
		}
	}

	/// <summary>
	/// Webhook for Stripe events
	/// </summary>
	[HttpPost("webhook")]
	public async Task<IActionResult> StripeWebhook()
	{
		var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
		var stripeSignature = Request.Headers["Stripe-Signature"];
		var webhookSecret = _config["Stripe:WebhookSecret"];

		try
		{
			var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

			if (stripeEvent.Type == "checkout.session.completed")
			{
				var session = stripeEvent.Data.Object as Session;

				if (session != null && long.TryParse(session.ClientReferenceId, out var transactionId))
				{
					var transaction = await _context.Transactions
						.Include(t => t.Advert)
						.FirstOrDefaultAsync(t => t.TransactionId == transactionId);

					if (transaction != null && transaction.Status == Enums.TransactionStatus.PENDING_PAYMENT)
					{
						transaction.Status = Enums.TransactionStatus.PAID_WAITING_SHIPPING;
						transaction.StripePaymentIntentId = session.PaymentIntentId;
						transaction.Advert.Status = Enums.AdvertStatus.SOLD;

						// Remove sold item from all carts
						var cartItems = await _context.CartItems.Where(c => c.AdvertId == transaction.AdvertId).ToListAsync();
						_context.CartItems.RemoveRange(cartItems);

						await _context.SaveChangesAsync();
					}
				}
			}

			return Ok();
		}
		catch (StripeException e)
		{
			return BadRequest(new { error = e.Message });
		}
	}
}
