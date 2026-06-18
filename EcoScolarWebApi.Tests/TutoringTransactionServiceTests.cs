using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests;

public class TutoringTransactionServiceTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly TutoringTransactionService _service;

    public TutoringTransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);
        // Refund is exercised in PaymentRefundServiceTests; here a substitute keeps the test focused.
        _service = new TutoringTransactionService(_context, Substitute.For<IRefundService>());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Transaction transaction, User seller, User buyer)> SeedWaitingAcceptanceAsync()
    {
        var seller = new User { Id = "seller-1", Nickname = "Tuteur", Email = "tuteur@test.ch", PhoneNumber = "0790000000" };
        var buyer = new User { Id = "buyer-1", Nickname = "Eleve" };
        var advert = new TutoringAdvert
        {
            AdvertId = 1,
            Title = "Cours de maths",
            Description = "Soutien",
            Price = 30m,
            SellerId = seller.Id,
            Seller = seller,
            Status = AdvertStatus.ACTIVE,
            StudyLevel = "Lycee",
            SubjectId = 1,
            SchoolGradeId = 1,
            TeachingLanguage = LanguageEnum.FR,
            MaxHours = 10
        };
        var transaction = new Transaction
        {
            TransactionId = 100,
            BuyerId = buyer.Id,
            AdvertId = advert.AdvertId,
            Advert = advert,
            Status = TransactionStatus.PAID_WAITING_ACCEPTANCE,
            PlatformFee = 1.50m,
            Quantity = 5,
            UnitPrice = 30m,
            Amount = 150m
        };
        _context.Users.AddRange(seller, buyer);
        _context.Adverts.Add(advert);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return (transaction, seller, buyer);
    }

    [Fact]
    public async Task AcceptAsync_ShouldMoveToPaidWaitingCompletion_WhenSellerAccepts()
    {
        var (transaction, seller, _) = await SeedWaitingAcceptanceAsync();

        var result = await _service.AcceptAsync(transaction.TransactionId, seller.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _context.Transactions.FindAsync(transaction.TransactionId);
        updated!.Status.Should().Be(TransactionStatus.PAID_WAITING_COMPLETION);
    }

    [Fact]
    public async Task RefuseAsync_ShouldCancelAndReactivateAdvert_WhenSellerRefuses()
    {
        var (transaction, seller, _) = await SeedWaitingAcceptanceAsync();

        var result = await _service.RefuseAsync(transaction.TransactionId, seller.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _context.Transactions.FindAsync(transaction.TransactionId);
        updated!.Status.Should().Be(TransactionStatus.CANCELLED);
        var advert = await _context.Adverts.FindAsync(transaction.AdvertId);
        advert!.Status.Should().Be(AdvertStatus.ACTIVE);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldSetBuyerConsent_WhenBuyerConfirmsCompletion()
    {
        var (transaction, seller, buyer) = await SeedWaitingAcceptanceAsync();
        await _service.AcceptAsync(transaction.TransactionId, seller.Id);

        var result = await _service.ConfirmAsync(transaction.TransactionId, buyer.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _context.Transactions.FindAsync(transaction.TransactionId);
        updated!.BuyerConsent.Should().BeTrue();
        updated.Status.Should().Be(TransactionStatus.PAID_WAITING_COMPLETION);
    }

    [Fact]
    public async Task MarkRenderedAsync_ShouldSetSellerConsentAndTimestamp_WhenSellerMarksRendered()
    {
        var (transaction, seller, buyer) = await SeedWaitingAcceptanceAsync();
        await _service.AcceptAsync(transaction.TransactionId, seller.Id);

        var result = await _service.MarkRenderedAsync(transaction.TransactionId, seller.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _context.Transactions.FindAsync(transaction.TransactionId);
        updated!.SellerConsent.Should().BeTrue();
        updated.TutorConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTutorContactAsync_ShouldReturnSellerContact_WhenBuyerRequestsAfterAcceptance()
    {
        var (transaction, seller, buyer) = await SeedWaitingAcceptanceAsync();
        await _service.AcceptAsync(transaction.TransactionId, seller.Id);

        var result = await _service.GetTutorContactAsync(transaction.TransactionId, buyer.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Tuteur");
        result.Data.Email.Should().Be("tuteur@test.ch");
        result.Data.PhoneNumber.Should().Be("0790000000");
    }

    [Fact]
    public async Task GetTutorContactAsync_ShouldFail_WhenTransactionNotAcceptedYet()
    {
        var (transaction, _, buyer) = await SeedWaitingAcceptanceAsync();

        var result = await _service.GetTutorContactAsync(transaction.TransactionId, buyer.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.BadRequest);
    }
}
