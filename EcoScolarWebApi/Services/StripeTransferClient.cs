using EcoScolarWebApi.Services.Contracts;
using Stripe;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Default <see cref="IStripeTransferClient"/> backed by the real Stripe SDK.
/// </summary>
public class StripeTransferClient : IStripeTransferClient
{
    public Task<Transfer> CreateTransferAsync(TransferCreateOptions options, CancellationToken cancellationToken = default)
        => new TransferService().CreateAsync(options, cancellationToken: cancellationToken);
}
