using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stripe.Checkout;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class PaymentServiceTests : IDisposable
{
    private const string BuyerId = "buyer-1";
    private const string SellerId = "seller-1";
    private const string BaseUrl = "http://localhost:3000";

    private readonly EcoscolarDbContext _context;
    private readonly IPlatformFeeCalculator _feeCalculator;
    private readonly IStripeCheckoutClient _checkoutClient;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        // Deterministic 10% fee, matching the configured default.
        _feeCalculator = Substitute.For<IPlatformFeeCalculator>();
        _feeCalculator.CalculateFee(Arg.Any<decimal>())
            .Returns(ci => Math.Round((decimal)ci[0] * 0.1m, 2, MidpointRounding.AwayFromZero));

        _checkoutClient = Substitute.For<IStripeCheckoutClient>();
        _checkoutClient.CreateSessionAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { Id = "cs_test_123", Url = "https://stripe.test/checkout" });

        _service = new PaymentService(_context, _feeCalculator, new ShippingFeeCalculator(), _checkoutClient, Substitute.For<ILogger<PaymentService>>());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<PhysicalItem> SeedAdvertAsync(decimal price = 30m, AdvertStatus status = AdvertStatus.ACTIVE, string sellerId = SellerId)
    {
        var advert = new PhysicalItem
        {
            Title = "A book",
            Description = "desc",
            Price = price,
            Status = status,
            SellerId = sellerId,
            NotificationDate = DateTime.UtcNow,
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();
        return advert;
    }

    // === checkout ===

    [Fact]
    public async Task CreateCheckoutSession_PricesServerSide_AndCreatesOnePendingTransactionPerAdvert()
    {
        var a1 = await SeedAdvertAsync(price: 30m);
        var a2 = await SeedAdvertAsync(price: 20m);

        var request = new CheckoutRequestDto { ProductIds = new List<long> { a1.AdvertId, a2.AdvertId } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Url.Should().Be("https://stripe.test/checkout");
        result.Data.OrderNumber.Should().NotBeNullOrWhiteSpace();

        var transactions = await _context.Transactions.OrderBy(t => t.UnitPrice).ToListAsync();
        transactions.Should().HaveCount(2);
        transactions.Should().OnlyContain(t => t.Status == TransactionStatus.PENDING);
        transactions.Should().OnlyContain(t => t.BuyerId == BuyerId);
        transactions.Should().OnlyContain(t => t.StripeSessionId == "cs_test_123");
        transactions.Should().OnlyContain(t => t.Quantity == 1);
        // All lines of the cart share the same order number (the transfer group at payout time).
        transactions.Select(t => t.OrderNumber).Distinct().Should().ContainSingle()
            .Which.Should().Be(result.Data.OrderNumber);

        var line20 = transactions[0];
        line20.UnitPrice.Should().Be(20m);
        line20.PlatformFee.Should().Be(2m);
        line20.Amount.Should().Be(22m); // unit + fee

        var line30 = transactions[1];
        line30.UnitPrice.Should().Be(30m);
        line30.PlatformFee.Should().Be(3m);
        line30.Amount.Should().Be(33m);
    }

    [Fact]
    public async Task CreateCheckoutSession_PausesTheAdverts()
    {
        var advert = await SeedAdvertAsync();
        var request = new CheckoutRequestDto { ProductId = (int)advert.AdvertId };

        await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        (await _context.Adverts.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.PAUSED);
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsNotFound_WhenAdvertMissing()
    {
        var request = new CheckoutRequestDto { ProductIds = new List<long> { 999 } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        await _checkoutClient.DidNotReceive().CreateSessionAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsConflict_WhenAdvertAlreadyPausedOrSold()
    {
        var advert = await SeedAdvertAsync(status: AdvertStatus.SOLD);
        var request = new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsConflict_WhenBuyingOwnAdvert()
    {
        var advert = await SeedAdvertAsync(sellerId: BuyerId);
        var request = new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateCheckoutSession_RejectsTutoringAdvert()
    {
        var tutoring = new TutoringAdvert
        {
            Title = "Maths",
            Description = "desc",
            Price = 40m,
            Status = AdvertStatus.ACTIVE,
            SellerId = SellerId,
            NotificationDate = DateTime.UtcNow,
            SubjectId = 1,
            SchoolGradeId = 1,
        };
        _context.Adverts.Add(tutoring);
        await _context.SaveChangesAsync();

        var request = new CheckoutRequestDto { ProductIds = new List<long> { tutoring.AdvertId } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        (await _context.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CreateCheckoutSession_DoesNotTouchDb_WhenStripeFails()
    {
        var advert = await SeedAdvertAsync();
        _checkoutClient.CreateSessionAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns<Session>(_ => throw new Stripe.StripeException("boom"));

        var request = new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } };

        var result = await _service.CreateCheckoutSessionAsync(request, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.InternalError);
        (await _context.Transactions.AnyAsync()).Should().BeFalse();
        (await _context.Adverts.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.ACTIVE);
    }

    // === webhook confirm ===

    [Fact]
    public async Task ConfirmCheckoutSession_MarksTransactionsPaid_AndAdvertSold()
    {
        var advert = await SeedAdvertAsync();
        await _service.CreateCheckoutSessionAsync(new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } }, BuyerId, BaseUrl);

        await _service.ConfirmCheckoutSessionAsync("cs_test_123", "pi_123");

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.PAID_WAITING_SHIPPING);
        transaction.StripePaymentIntentId.Should().Be("pi_123");
        (await _context.Adverts.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.SOLD);
    }

    [Fact]
    public async Task ConfirmCheckoutSession_IsIdempotent_OnReplay()
    {
        var advert = await SeedAdvertAsync();
        await _service.CreateCheckoutSessionAsync(new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } }, BuyerId, BaseUrl);

        await _service.ConfirmCheckoutSessionAsync("cs_test_123", "pi_123");
        // Stripe replays the same event with a different (later) payment intent reference must not corrupt state.
        await _service.ConfirmCheckoutSessionAsync("cs_test_123", "pi_replayed");

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.PAID_WAITING_SHIPPING);
        transaction.StripePaymentIntentId.Should().Be("pi_123");
    }

    [Fact]
    public async Task ConfirmCheckoutSession_UnknownSession_IsNoOp()
    {
        var act = async () => await _service.ConfirmCheckoutSessionAsync("cs_unknown", "pi_x");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfirmCheckoutSession_TutoringAdvert_BecomesWaitingAcceptance_AndAdvertStaysActive()
    {
        var tutoring = new TutoringAdvert
        {
            Title = "Maths",
            Description = "desc",
            Price = 30m,
            Status = AdvertStatus.ACTIVE,
            SellerId = SellerId,
            NotificationDate = DateTime.UtcNow,
            SubjectId = 1,
            SchoolGradeId = 1,
        };
        _context.Adverts.Add(tutoring);
        await _context.SaveChangesAsync();

        _context.Transactions.Add(new Transaction
        {
            AdvertId = tutoring.AdvertId,
            BuyerId = BuyerId,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.PENDING,
            StripeSessionId = "cs_tut_1",
            Quantity = 5,
            UnitPrice = 30m,
            PlatformFee = 15m,
            Amount = 165m,
        });
        await _context.SaveChangesAsync();

        await _service.ConfirmCheckoutSessionAsync("cs_tut_1", "pi_tut");

        var transaction = await _context.Transactions.SingleAsync();
        // Tutoring waits for the tutor's acceptance and the advert is never marked SOLD.
        transaction.Status.Should().Be(TransactionStatus.PAID_WAITING_ACCEPTANCE);
        transaction.StripePaymentIntentId.Should().Be("pi_tut");
        (await _context.Adverts.FindAsync(tutoring.AdvertId))!.Status.Should().Be(AdvertStatus.ACTIVE);
    }

    // === webhook cancel / revert ===

    [Fact]
    public async Task CancelCheckoutSession_RevertsPendingTransactionsAndReactivatesAdvert()
    {
        var advert = await SeedAdvertAsync();
        await _service.CreateCheckoutSessionAsync(new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } }, BuyerId, BaseUrl);

        await _service.CancelCheckoutSessionAsync("cs_test_123");

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.CANCELLED);
        (await _context.Adverts.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.ACTIVE);
    }

    [Fact]
    public async Task CancelCheckoutSession_DoesNotRevertAlreadyPaidTransaction()
    {
        var advert = await SeedAdvertAsync();
        await _service.CreateCheckoutSessionAsync(new CheckoutRequestDto { ProductIds = new List<long> { advert.AdvertId } }, BuyerId, BaseUrl);
        await _service.ConfirmCheckoutSessionAsync("cs_test_123", "pi_123");

        // A late expiry event after the payment succeeded must not cancel a paid order.
        await _service.CancelCheckoutSessionAsync("cs_test_123");

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.PAID_WAITING_SHIPPING);
        (await _context.Adverts.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.SOLD);
    }
}
