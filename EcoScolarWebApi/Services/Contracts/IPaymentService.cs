using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Server-authoritative checkout and webhook handling.
/// A cart maps to a single Stripe Checkout session but to one transaction per advert
/// (each advert belongs to a single seller); all lines of an order share the same
/// order number, reused as the Stripe transfer group for the per-seller payout.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Validates and prices the adverts server-side, creates one pending transaction per advert,
    /// pauses the adverts and returns the Stripe Checkout session URL.
    /// </summary>
    Task<Result<CheckoutSessionResultDto>> CreateCheckoutSessionAsync(CheckoutRequestDto request, string buyerId, string baseUrl);

    /// <summary>
    /// Applies a paid checkout session: pending transactions become PAID_WAITING_SHIPPING
    /// and their adverts SOLD. Idempotent.
    /// </summary>
    Task ConfirmCheckoutSessionAsync(string sessionId, string? paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts an expired or failed session: pending transactions become CANCELLED
    /// and their adverts are reactivated. Idempotent.
    /// </summary>
    Task CancelCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
