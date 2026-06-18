using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Tutoring;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Services;

public class TutoringTransactionService(EcoscolarDbContext context, IRefundService refundService) : ITutoringTransactionService
{
    private readonly EcoscolarDbContext _context = context;
    private readonly IRefundService _refundService = refundService;

    public async Task<Result> AcceptAsync(long transactionId, string sellerId)
    {
        var transaction = await LoadTutoringTransactionAsync(transactionId);
        if (transaction is null)
            return Result.Failure("Transaction introuvable.", ErrorType.NotFound);

        if (transaction.Advert.SellerId != sellerId)
            return Result.Failure("Accès refusé.", ErrorType.Forbidden);

        if (transaction.Status != TransactionStatus.PAID_WAITING_ACCEPTANCE)
            return Result.Failure("La transaction n'est pas en attente d'acceptation.", ErrorType.BadRequest);

        transaction.Status = TransactionStatus.PAID_WAITING_COMPLETION;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> RefuseAsync(long transactionId, string sellerId)
    {
        var transaction = await LoadTutoringTransactionAsync(transactionId);
        if (transaction is null)
            return Result.Failure("Transaction introuvable.", ErrorType.NotFound);

        if (transaction.Advert.SellerId != sellerId)
            return Result.Failure("Accès refusé.", ErrorType.Forbidden);

        if (transaction.Status != TransactionStatus.PAID_WAITING_ACCEPTANCE)
            return Result.Failure("La transaction n'est pas en attente d'acceptation.", ErrorType.BadRequest);

        await _refundService.RefundAsync(transaction);

        transaction.Status = TransactionStatus.CANCELLED;
        transaction.Advert.Status = AdvertStatus.ACTIVE;

        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ConfirmAsync(long transactionId, string buyerId)
    {
        var transaction = await LoadTutoringTransactionAsync(transactionId);
        if (transaction is null)
            return Result.Failure("Transaction introuvable.", ErrorType.NotFound);

        if (transaction.BuyerId != buyerId)
            return Result.Failure("Accès refusé.", ErrorType.Forbidden);

        if (transaction.Status != TransactionStatus.PAID_WAITING_COMPLETION)
            return Result.Failure("La prestation doit être acceptée par le tuteur avant confirmation.", ErrorType.BadRequest);

        if (transaction.BuyerConsent)
            return Result.Failure("La conclusion a déjà été confirmée par l'acheteur.", ErrorType.BadRequest);

        transaction.BuyerConsent = true;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> MarkRenderedAsync(long transactionId, string sellerId)
    {
        var transaction = await LoadTutoringTransactionAsync(transactionId);
        if (transaction is null)
            return Result.Failure("Transaction introuvable.", ErrorType.NotFound);

        if (transaction.Advert.SellerId != sellerId)
            return Result.Failure("Accès refusé.", ErrorType.Forbidden);

        if (transaction.Status != TransactionStatus.PAID_WAITING_COMPLETION)
            return Result.Failure("La prestation doit être acceptée par le tuteur.", ErrorType.BadRequest);

        if (transaction.SellerConsent)
            return Result.Failure("La prestation a déjà été déclarée comme rendue.", ErrorType.BadRequest);

        transaction.SellerConsent = true;
        transaction.TutorConfirmedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<TutorContactDto>> GetTutorContactAsync(long transactionId, string buyerId)
    {
        var transaction = await LoadTutoringTransactionAsync(transactionId);
        if (transaction is null)
            return Result<TutorContactDto>.Failure("Transaction introuvable.", ErrorType.NotFound);

        if (transaction.BuyerId != buyerId)
            return Result<TutorContactDto>.Failure("Accès refusé.", ErrorType.Forbidden);

        if (transaction.Status != TransactionStatus.PAID_WAITING_COMPLETION)
            return Result<TutorContactDto>.Failure("Les coordonnées du tuteur ne sont accessibles qu'après acceptation.", ErrorType.BadRequest);

        var seller = transaction.Advert.Seller;
        var name = seller.Nickname
            ?? (!string.IsNullOrWhiteSpace(seller.FirstName) || !string.IsNullOrWhiteSpace(seller.LastName)
                ? $"{seller.FirstName} {seller.LastName}".Trim()
                : seller.UserName ?? "Anonyme");

        return Result<TutorContactDto>.Success(new TutorContactDto(
            name,
            seller.PhoneNumber,
            seller.Email
        ));
    }

    private async Task<Transaction?> LoadTutoringTransactionAsync(long transactionId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction?.Advert is not TutoringAdvert)
            return null;

        return transaction;
    }
}
