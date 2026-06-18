using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Core of the tutoring escrow flow (Étape G). Resolves due packages and moves the money:
/// refunds on acceptance timeout, releases to the tutor on student confirmation /
/// post-mark-rendered delay / package expiry. Reuses <see cref="IPayoutService"/> (P5) and
/// <see cref="IRefundService"/> (P6). The status PAID_WAITING_* are tutoring-only, so filtering
/// by them already restricts to tutoring transactions.
/// </summary>
public class TutoringEscrowProcessor(
    EcoscolarDbContext context,
    IPayoutService payoutService,
    IRefundService refundService,
    IConfiguration configuration,
    ILogger<TutoringEscrowProcessor> logger) : ITutoringEscrowProcessor
{
    private const int DefaultAcceptanceDeadlineDays = 15;
    private const int DefaultAutoReleaseDays = 15;

    public async Task ProcessDueTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var acceptanceCutoff = now.AddDays(-configuration.GetValue("BusinessSettings:TutorAcceptanceDeadlineDays", DefaultAcceptanceDeadlineDays));
        var autoReleaseCutoff = now.AddDays(-configuration.GetValue("BusinessSettings:TutoringAutoReleaseDays", DefaultAutoReleaseDays));

        var changed = false;

        // 1) Acceptance deadline passed without the tutor accepting → refund the student + cancel.
        //    The advert stays ACTIVE (it never left ACTIVE for tutoring).
        var expiredAcceptance = await context.Transactions
            .Include(t => t.Advert).ThenInclude(a => a.Seller)
            .Where(t => t.Status == TransactionStatus.PAID_WAITING_ACCEPTANCE && t.Date < acceptanceCutoff)
            .ToListAsync(cancellationToken);

        foreach (var transaction in expiredAcceptance)
        {
            await refundService.RefundAsync(transaction, cancellationToken);
            transaction.Status = TransactionStatus.CANCELLED;
            if (transaction.Advert is not null)
                transaction.Advert.Status = AdvertStatus.ACTIVE;
            changed = true;
            logger.LogInformation("Tutoring transaction {TransactionId}: acceptance deadline passed → refunded & cancelled.", transaction.TransactionId);
        }

        // 2) Accepted packages ready to release to the tutor.
        var inEscrow = await context.Transactions
            .Include(t => t.Advert).ThenInclude(a => a.Seller)
            .Where(t => t.Status == TransactionStatus.PAID_WAITING_COMPLETION)
            .ToListAsync(cancellationToken);

        foreach (var transaction in inEscrow)
        {
            var dueByStudent = transaction.BuyerConsent;
            var dueByTutorDelay = transaction.SellerConsent
                && transaction.TutorConfirmedAt.HasValue
                && transaction.TutorConfirmedAt.Value < autoReleaseCutoff;
            var dueByExpiry = transaction.PackageExpiresAt.HasValue && transaction.PackageExpiresAt.Value < now;

            if (!(dueByStudent || dueByTutorDelay || dueByExpiry))
                continue;

            await payoutService.ReleaseFundsAsync(transaction, cancellationToken);
            transaction.Status = TransactionStatus.COMPLETED;
            // The tutoring advert stays ACTIVE (never SOLD).
            changed = true;
            logger.LogInformation(
                "Tutoring transaction {TransactionId}: released to tutor (student={Student}, tutorDelay={Delay}, expiry={Expiry}).",
                transaction.TransactionId, dueByStudent, dueByTutorDelay, dueByExpiry);
        }

        if (changed)
            await context.SaveChangesAsync(cancellationToken);
    }
}
