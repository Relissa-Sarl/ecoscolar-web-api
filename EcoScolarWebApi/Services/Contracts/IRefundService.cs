using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Refunds the buyer for a transaction (full refund on the captured PaymentIntent).
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Issues a full Stripe refund on the transaction's PaymentIntent. Returns a failure result
    /// if no PaymentIntent is available or Stripe fails. The caller owns the status transition
    /// (e.g. CANCELLED) and SaveChanges.
    /// </summary>
    Task<Result> RefundAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
