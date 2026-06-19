using Stripe;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Thin abstraction over the Stripe <see cref="TransferService"/> so payouts can be unit
/// tested without calling Stripe.
/// </summary>
public interface IStripeTransferClient
{
    Task<Transfer> CreateTransferAsync(TransferCreateOptions options, CancellationToken cancellationToken = default);
}
