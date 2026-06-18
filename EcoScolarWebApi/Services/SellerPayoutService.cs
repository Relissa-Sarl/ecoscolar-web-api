using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Centralises the seller payout (Stripe Connect transfer). Replaces the manual, hard-coded
/// transfers previously inlined in <c>TransactionsController.ConfirmReceipt</c> and
/// <c>AutoConfirmReceiptService</c>. See <see cref="IPayoutService"/>.
/// </summary>
public class SellerPayoutService(
    EcoscolarDbContext context,
    IStripeTransferClient transferClient,
    ILogger<SellerPayoutService> logger) : IPayoutService
{
    public async Task<Result> ReleaseFundsAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        // Idempotent: funds already released for this transaction (Stripe events / jobs may retry).
        if (!string.IsNullOrEmpty(transaction.StripeTransferId))
            return Result.Success();

        // Seller's connected account — use the loaded navigation if present, else fetch it.
        var stripeAccountId = transaction.Advert?.Seller?.StripeAccountId
            ?? await context.Adverts
                .Where(a => a.AdvertId == transaction.AdvertId)
                .Select(a => a.Seller!.StripeAccountId)
                .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(stripeAccountId))
        {
            logger.LogWarning("Transaction {TransactionId}: seller has no connected Stripe account; payout skipped.", transaction.TransactionId);
            return Result.Failure("Seller has no connected Stripe account.", ErrorType.Conflict);
        }

        // The seller receives the amount paid by the buyer net of the platform commission.
        var sellerAmount = transaction.Amount - transaction.PlatformFee;
        if (sellerAmount <= 0)
        {
            logger.LogWarning("Transaction {TransactionId}: non-positive payout amount ({Amount} CHF); skipped.", transaction.TransactionId, sellerAmount);
            return Result.Failure("Non-positive payout amount.", ErrorType.BadRequest);
        }

        var options = new TransferCreateOptions
        {
            Amount = (long)Math.Round(sellerAmount * 100, MidpointRounding.AwayFromZero),
            Currency = "chf",
            Destination = stripeAccountId,
            // The order number ties this payout to its order (multi-seller orders reconcile under one group).
            TransferGroup = transaction.OrderNumber,
        };

        try
        {
            var transfer = await transferClient.CreateTransferAsync(options, cancellationToken);
            transaction.StripeTransferId = transfer.Id;
            logger.LogInformation(
                "Transaction {TransactionId}: transferred {Amount} CHF to {Account} (transfer {TransferId}).",
                transaction.TransactionId, sellerAmount, stripeAccountId, transfer.Id);
            return Result.Success();
        }
        catch (StripeException e)
        {
            logger.LogError(e, "Transaction {TransactionId}: Stripe transfer failed.", transaction.TransactionId);
            return Result.Failure(e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
        }
    }
}
