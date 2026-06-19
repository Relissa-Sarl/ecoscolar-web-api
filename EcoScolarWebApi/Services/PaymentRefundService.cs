using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Stripe;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Centralised buyer refund (Stripe Refund on the PaymentIntent). Replaces the ad-hoc inline
/// refund previously in <c>TutoringTransactionService</c>. See <see cref="IRefundService"/>.
/// The webhook stores <c>StripePaymentIntentId</c> on payment, so a paid transaction always has one.
/// </summary>
public class PaymentRefundService(
    IStripeRefundClient refundClient,
    ILogger<PaymentRefundService> logger) : IRefundService
{
    public async Task<Result> RefundAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(transaction.StripePaymentIntentId))
        {
            logger.LogWarning("Transaction {TransactionId}: no PaymentIntent to refund.", transaction.TransactionId);
            return Result.Failure("No payment intent to refund.", ErrorType.Conflict);
        }

        try
        {
            var refund = await refundClient.CreateRefundAsync(
                new RefundCreateOptions { PaymentIntent = transaction.StripePaymentIntentId },
                cancellationToken);
            logger.LogInformation("Transaction {TransactionId}: refunded (refund {RefundId}).", transaction.TransactionId, refund.Id);
            return Result.Success();
        }
        catch (StripeException e)
        {
            logger.LogError(e, "Transaction {TransactionId}: Stripe refund failed.", transaction.TransactionId);
            return Result.Failure(e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
        }
    }
}
