using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Releases escrowed funds to a seller for a completed transaction.
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Transfers the seller's share (amount net of the platform fee) to their connected
    /// Stripe account, using the order number as the transfer group.
    /// <para>
    /// Idempotent: a no-op if the transaction already has a <c>StripeTransferId</c>.
    /// On success, sets <c>transaction.StripeTransferId</c>; the caller is responsible for
    /// persisting the change (SaveChanges), together with the status transition.
    /// </para>
    /// </summary>
    Task<Result> ReleaseFundsAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
