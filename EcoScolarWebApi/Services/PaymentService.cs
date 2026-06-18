using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Server-authoritative payments service. See <see cref="IPaymentService"/> for the multi-vendor model.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly EcoscolarDbContext _context;
    private readonly IPlatformFeeCalculator _feeCalculator;
    private readonly IShippingFeeCalculator _shippingFeeCalculator;
    private readonly IStripeCheckoutClient _checkoutClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        EcoscolarDbContext context,
        IPlatformFeeCalculator feeCalculator,
        IShippingFeeCalculator shippingFeeCalculator,
        IStripeCheckoutClient checkoutClient,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _feeCalculator = feeCalculator;
        _shippingFeeCalculator = shippingFeeCalculator;
        _checkoutClient = checkoutClient;
        _logger = logger;
    }

    public async Task<Result<CheckoutSessionResultDto>> CreateCheckoutSessionAsync(CheckoutRequestDto request, string buyerId, string baseUrl)
    {
        var advertIds = (request.ProductIds is { Count: > 0 } ? request.ProductIds : new List<long> { request.ProductId })
            .Distinct()
            .ToList();

        if (advertIds.Count == 0)
            return Result<CheckoutSessionResultDto>.Failure("Aucun article à payer.", ErrorType.BadRequest);

        // Load every advert in a single round-trip; track them so we can pause them below.
        var adverts = await _context.Adverts
            .Where(a => advertIds.Contains(a.AdvertId))
            .ToListAsync();

        // Build the priced lines, validating each advert. Prices come from the DB, never from the client.
        var lines = new List<(Advert Advert, decimal UnitPrice, decimal Fee, decimal Amount)>();
        foreach (var advertId in advertIds)
        {
            var advert = adverts.FirstOrDefault(a => a.AdvertId == advertId);
            if (advert is null)
                return Result<CheckoutSessionResultDto>.Failure($"L'annonce avec l'ID {advertId} n'existe pas.", ErrorType.NotFound);

            // Tutoring is sold through a dedicated reservation flow (hours + escrow), never the cart checkout.
            if (advert is TutoringAdvert)
                return Result<CheckoutSessionResultDto>.Failure("Les cours d'appui se réservent directement depuis l'annonce, pas via le panier.", ErrorType.Conflict);

            if (advert.Status is AdvertStatus.PAUSED or AdvertStatus.SOLD)
                return Result<CheckoutSessionResultDto>.Failure("Un des articles dans votre panier est en cours de paiement ou déjà vendu.", ErrorType.Conflict);

            if (advert.SellerId == buyerId)
                return Result<CheckoutSessionResultDto>.Failure("Vous ne pouvez pas acheter votre propre annonce.", ErrorType.Conflict);

            var unitPrice = advert.Price;
            var fee = _feeCalculator.CalculateFee(unitPrice);
            lines.Add((advert, unitPrice, fee, unitPrice + fee));
        }

        // Order total charged to the buyer (escrowed on the platform account).
        // The platform is not subject to VAT, so none is applied.
        var subtotal = lines.Sum(l => l.UnitPrice);
        var totalFee = lines.Sum(l => l.Fee);
        var shipping = _shippingFeeCalculator.CalculateFee(request.ShippingMethod);
        var grandTotal = subtotal + shipping + totalFee;
        var amountInCents = (long)Math.Round(grandTotal * 100, MidpointRounding.AwayFromZero);

        var orderNumber = await GenerateUniqueOrderNumberAsync();
        var advertIdsQuery = string.Join(",", advertIds);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amountInCents,
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
            // Separate charges & transfers: the order number ties the (possibly multi-seller) transfers together at payout time.
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                TransferGroup = orderNumber,
                Metadata = new Dictionary<string, string>
                {
                    ["orderNumber"] = orderNumber,
                    ["buyerId"] = buyerId,
                },
            },
            Metadata = new Dictionary<string, string>
            {
                ["orderNumber"] = orderNumber,
                ["buyerId"] = buyerId,
            },
            SuccessUrl = $"{baseUrl}/success?productIds={advertIdsQuery}&orderId={orderNumber}",
            CancelUrl = $"{baseUrl}/denied?productIds={advertIdsQuery}",
        };

        Session session;
        try
        {
            session = await _checkoutClient.CreateSessionAsync(options);
        }
        catch (Stripe.StripeException e)
        {
            _logger.LogError(e, "Stripe checkout session creation failed for order {OrderNumber}.", orderNumber);
            return Result<CheckoutSessionResultDto>.Failure(e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
        }

        // Only mutate the DB once Stripe accepted the session: one PENDING transaction per advert, all sharing the order number.
        var now = DateTime.UtcNow;
        foreach (var line in lines)
        {
            line.Advert.Status = AdvertStatus.PAUSED;

            _context.Transactions.Add(new Transaction
            {
                AdvertId = line.Advert.AdvertId,
                BuyerId = buyerId,
                Date = now,
                Status = TransactionStatus.PENDING,
                OrderNumber = orderNumber,
                StripeSessionId = session.Id,
                Quantity = 1,
                UnitPrice = line.UnitPrice,
                PlatformFee = line.Fee,
                Amount = line.Amount,
            });
        }

        await _context.SaveChangesAsync();

        return Result<CheckoutSessionResultDto>.Success(new CheckoutSessionResultDto(session.Url, orderNumber));
    }

    public async Task ConfirmCheckoutSessionAsync(string sessionId, string? paymentIntentId, CancellationToken cancellationToken = default)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Advert)
            .Where(t => t.StripeSessionId == sessionId)
            .ToListAsync(cancellationToken);

        var pending = transactions.Where(t => t.Status == TransactionStatus.PENDING).ToList();
        if (pending.Count == 0)
        {
            // Idempotent: the event was already processed (Stripe replays events), nothing to do.
            _logger.LogInformation("Checkout session {SessionId} already confirmed or unknown; skipping.", sessionId);
            return;
        }

        foreach (var transaction in pending)
        {
            transaction.StripePaymentIntentId = paymentIntentId;

            if (transaction.Advert is TutoringAdvert)
            {
                // Tutoring package: awaits the tutor's accept/refuse decision (UC-09 E6-02).
                // The advert stays ACTIVE so other students can keep booking the same tutor.
                transaction.Status = TransactionStatus.PAID_WAITING_ACCEPTANCE;
            }
            else
            {
                // Physical goods: paid, waiting for the seller to ship.
                transaction.Status = TransactionStatus.PAID_WAITING_SHIPPING;
                if (transaction.Advert is not null)
                    transaction.Advert.Status = AdvertStatus.SOLD;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Confirmed {Count} transaction(s) for checkout session {SessionId}.", pending.Count, sessionId);
    }

    public async Task CancelCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var pending = await _context.Transactions
            .Include(t => t.Advert)
            .Where(t => t.StripeSessionId == sessionId && t.Status == TransactionStatus.PENDING)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        foreach (var transaction in pending)
        {
            transaction.Status = TransactionStatus.CANCELLED;
            // Reactivate the advert that was paused for checkout so it can be sold again.
            if (transaction.Advert is not null && transaction.Advert.Status == AdvertStatus.PAUSED)
                transaction.Advert.Status = AdvertStatus.ACTIVE;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled {Count} pending transaction(s) for checkout session {SessionId}.", pending.Count, sessionId);
    }

    private async Task<string> GenerateUniqueOrderNumberAsync()
    {
        string orderNumber;
        do
        {
            orderNumber = $"ECO-{DateTime.UtcNow:yyyyMMdd}-{System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
        }
        while (await _context.Transactions.AnyAsync(t => t.OrderNumber == orderNumber));
        return orderNumber;
    }
}
