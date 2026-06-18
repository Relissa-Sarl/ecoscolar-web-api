using Stripe;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Thin abstraction over the Stripe <see cref="RefundService"/> so refunds can be unit tested
/// without calling Stripe.
/// </summary>
public interface IStripeRefundClient
{
    Task<Refund> CreateRefundAsync(RefundCreateOptions options, CancellationToken cancellationToken = default);
}
