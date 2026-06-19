using EcoScolarWebApi.Services.Contracts;
using Stripe;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Default <see cref="IStripeRefundClient"/> backed by the real Stripe SDK.
/// </summary>
public class StripeRefundClient : IStripeRefundClient
{
    public Task<Refund> CreateRefundAsync(RefundCreateOptions options, CancellationToken cancellationToken = default)
        => new RefundService().CreateAsync(options, cancellationToken: cancellationToken);
}
