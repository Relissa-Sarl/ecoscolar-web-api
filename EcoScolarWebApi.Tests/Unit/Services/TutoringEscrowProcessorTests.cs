using Xunit;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class TutoringEscrowProcessorTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly IPayoutService _payoutService;
    private readonly IRefundService _refundService;
    private readonly TutoringEscrowProcessor _processor;

    public TutoringEscrowProcessorTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        _payoutService = Substitute.For<IPayoutService>();
        _refundService = Substitute.For<IRefundService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BusinessSettings:TutorAcceptanceDeadlineDays"] = "15",
                ["BusinessSettings:TutoringAutoReleaseDays"] = "15",
            })
            .Build();

        _processor = new TutoringEscrowProcessor(
            _context, _payoutService, _refundService, config,
            Substitute.For<ILogger<TutoringEscrowProcessor>>());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<long> SeedAsync(TransactionStatus status, Action<Transaction> configure)
    {
        var seller = new User
        {
            Id = $"seller-{Guid.NewGuid():N}",
            UserName = $"s-{Guid.NewGuid():N}@test.ch",
            Email = $"s-{Guid.NewGuid():N}@test.ch",
            StripeAccountId = "acct_seller",
        };
        _context.Users.Add(seller);

        var advert = new TutoringAdvert
        {
            Title = "Cours de maths",
            Description = "desc",
            Price = 30m,
            Status = AdvertStatus.ACTIVE,
            SellerId = seller.Id,
            NotificationDate = DateTime.UtcNow,
            SubjectId = 1,
            SchoolGradeId = 1,
            StudyLevel = "Lycee",
            TeachingLanguage = LanguageEnum.FR,
            MaxHours = 10,
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            BuyerId = "buyer-1",
            AdvertId = advert.AdvertId,
            Status = status,
            Date = DateTime.UtcNow,
            Quantity = 5,
            UnitPrice = 30m,
            Amount = 150m,
            PlatformFee = 1.5m,
            StripePaymentIntentId = "pi_1",
        };
        configure(transaction);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction.TransactionId;
    }

    [Fact]
    public async Task Process_RefundsAndCancels_WhenAcceptanceDeadlinePassed()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_ACCEPTANCE, t => t.Date = DateTime.UtcNow.AddDays(-20));

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.CANCELLED);
        await _refundService.Received(1).RefundAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_DoesNothing_WhenAcceptanceStillWithinDeadline()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_ACCEPTANCE, t => t.Date = DateTime.UtcNow.AddDays(-2));

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.PAID_WAITING_ACCEPTANCE);
        await _refundService.DidNotReceive().RefundAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_Releases_WhenStudentConfirmed()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_COMPLETION, t =>
        {
            t.BuyerConsent = true;
            t.PackageExpiresAt = DateTime.UtcNow.AddDays(10);
        });

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.COMPLETED);
        await _payoutService.Received(1).ReleaseFundsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_Releases_WhenTutorDeclaredRenderedAndDelayElapsed()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_COMPLETION, t =>
        {
            t.SellerConsent = true;
            t.TutorConfirmedAt = DateTime.UtcNow.AddDays(-20);
            t.PackageExpiresAt = DateTime.UtcNow.AddDays(10);
        });

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.COMPLETED);
        await _payoutService.Received(1).ReleaseFundsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_Releases_WhenPackageExpired()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_COMPLETION, t => t.PackageExpiresAt = DateTime.UtcNow.AddDays(-1));

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.COMPLETED);
        await _payoutService.Received(1).ReleaseFundsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_DoesNotRelease_WhenNeitherConfirmedNorExpired()
    {
        var id = await SeedAsync(TransactionStatus.PAID_WAITING_COMPLETION, t =>
        {
            t.BuyerConsent = false;
            t.SellerConsent = false;
            t.PackageExpiresAt = DateTime.UtcNow.AddDays(10);
        });

        await _processor.ProcessDueTransactionsAsync();

        var tx = await _context.Transactions.FindAsync(id);
        tx!.Status.Should().Be(TransactionStatus.PAID_WAITING_COMPLETION);
        await _payoutService.DidNotReceive().ReleaseFundsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }
}
