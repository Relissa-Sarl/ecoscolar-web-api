using EcoScolarWebApi.Services.Contracts;
using Stripe.Checkout;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Default <see cref="IStripeCheckoutClient"/> backed by the real Stripe SDK.
/// </summary>
public class StripeCheckoutClient : IStripeCheckoutClient
{
    public Task<Session> CreateSessionAsync(SessionCreateOptions options, CancellationToken cancellationToken = default)
        => new SessionService().CreateAsync(options, cancellationToken: cancellationToken);
}
