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
/// Tutoring reservation. Reuses the server-authoritative pricing primitives
/// (<see cref="IPlatformFeeCalculator"/>, <see cref="IStripeCheckoutClient"/>) but on a dedicated
/// path: tutoring is never sold through the cart. See <see cref="ITutoringReservationService"/>.
/// </summary>
public class TutoringReservationService(
    EcoscolarDbContext context,
    IPlatformFeeCalculator feeCalculator,
    IStripeCheckoutClient checkoutClient,
    IConfiguration configuration,
    ILogger<TutoringReservationService> logger) : ITutoringReservationService
{
    private const int DefaultPackageValidityDays = 15;
    private readonly int _packageValidityDays = configuration.GetValue("BusinessSettings:TutoringPackageValidityDays", DefaultPackageValidityDays);

    public async Task<Result<CheckoutSessionResultDto>> CreateReservationSessionAsync(long advertId, int hours, string buyerId, string baseUrl)
    {
        var advert = await context.Services.FirstOrDefaultAsync(a => a.AdvertId == advertId);
        if (advert is null)
            return Result<CheckoutSessionResultDto>.Failure("Le cours d'appui spécifié n'existe pas.", ErrorType.NotFound);

        if (advert.Status != AdvertStatus.ACTIVE)
            return Result<CheckoutSessionResultDto>.Failure("Ce cours d'appui n'est pas disponible à la réservation.", ErrorType.Conflict);

        if (advert.SellerId == buyerId)
            return Result<CheckoutSessionResultDto>.Failure("Vous ne pouvez pas réserver votre propre cours d'appui.", ErrorType.Conflict);

        var minHours = advert.MinHours ?? 1;
        if (hours < minHours || hours > advert.MaxHours)
            return Result<CheckoutSessionResultDto>.Failure(
                $"Le nombre d'heures doit être compris entre {minHours} et {advert.MaxHours}.", ErrorType.BadRequest);

        // Pricing is authoritative server-side: hourly rate × hours, plus the platform fee.
        var unitPrice = advert.Price;
        var subtotal = unitPrice * hours;
        var fee = feeCalculator.CalculateFee(subtotal);
        var total = subtotal + fee;
        var amountInCents = (long)Math.Round(total * 100, MidpointRounding.AwayFromZero);

        var orderNumber = await GenerateUniqueOrderNumberAsync();

        var metadata = new Dictionary<string, string>
        {
            ["orderNumber"] = orderNumber,
            ["buyerId"] = buyerId,
            ["advertId"] = advertId.ToString(),
            ["hours"] = hours.ToString(),
            ["type"] = "tutoring",
        };

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amountInCents,
                        Currency = "chf",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Réservation de cours d'appui",
                            Description = $"{hours} h — {advert.Title}",
                        },
                    },
                    Quantity = 1,
                },
            ],
            Mode = "payment",
            // Escrowed on the platform account; orderNumber is reused as the transfer group at payout time.
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                TransferGroup = orderNumber,
                Metadata = metadata,
            },
            Metadata = metadata,
            SuccessUrl = $"{baseUrl}/success?productIds={advertId}&orderId={orderNumber}",
            CancelUrl = $"{baseUrl}/denied?productIds={advertId}",
        };

        Session session;
        try
        {
            session = await checkoutClient.CreateSessionAsync(options);
        }
        catch (Stripe.StripeException e)
        {
            logger.LogError(e, "Stripe reservation session creation failed for tutoring advert {AdvertId}.", advertId);
            return Result<CheckoutSessionResultDto>.Failure(e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
        }

        // Only mutate the DB once Stripe accepted the session. The advert stays ACTIVE:
        // several students can hold packages on the same tutoring advert in parallel.
        var now = DateTime.UtcNow;
        context.Transactions.Add(new Transaction
        {
            AdvertId = advert.AdvertId,
            BuyerId = buyerId,
            Date = now,
            Status = TransactionStatus.PENDING,
            OrderNumber = orderNumber,
            StripeSessionId = session.Id,
            Quantity = hours,
            UnitPrice = unitPrice,
            PlatformFee = fee,
            Amount = total,
            PackageExpiresAt = now.AddDays(_packageValidityDays),
        });

        await context.SaveChangesAsync();

        return Result<CheckoutSessionResultDto>.Success(new CheckoutSessionResultDto(session.Url, orderNumber));
    }

    private async Task<string> GenerateUniqueOrderNumberAsync()
    {
        string orderNumber;
        do
        {
            orderNumber = $"ECO-{DateTime.UtcNow:yyyyMMdd}-{System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
        }
        while (await context.Transactions.AnyAsync(t => t.OrderNumber == orderNumber));
        return orderNumber;
    }
}
