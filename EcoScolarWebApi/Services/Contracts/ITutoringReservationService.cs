using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Dedicated reservation flow for tutoring adverts (hours credit + escrow), separate from the
/// cart checkout. Validates and prices the booking server-side, creates a single pending
/// transaction and returns the Stripe Checkout session URL. The transaction becomes
/// PAID_WAITING_ACCEPTANCE on payment (handled by the shared payment webhook).
/// </summary>
public interface ITutoringReservationService
{
    /// <summary>
    /// Validates the advert and hours, prices the package server-side, creates a PENDING
    /// transaction (advert stays ACTIVE) and returns the Stripe Checkout session URL.
    /// </summary>
    Task<Result<CheckoutSessionResultDto>> CreateReservationSessionAsync(long advertId, int hours, string buyerId, string baseUrl);
}
