using Stripe.Checkout;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Thin abstraction over the Stripe Checkout <see cref="SessionService"/> so checkout
/// can be unit tested without calling Stripe.
/// </summary>
public interface IStripeCheckoutClient
{
    Task<Session> CreateSessionAsync(SessionCreateOptions options, CancellationToken cancellationToken = default);
}
