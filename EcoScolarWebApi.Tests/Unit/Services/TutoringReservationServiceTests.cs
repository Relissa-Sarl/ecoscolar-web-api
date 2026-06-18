using Xunit;
using EcoScolarWebApi.Commun;
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
using Stripe.Checkout;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class TutoringReservationServiceTests : IDisposable
{
    private const string BuyerId = "buyer-1";
    private const string SellerId = "tutor-1";
    private const string BaseUrl = "http://localhost:3000";

    private readonly EcoscolarDbContext _context;
    private readonly IPlatformFeeCalculator _feeCalculator;
    private readonly IStripeCheckoutClient _checkoutClient;
    private readonly TutoringReservationService _service;

    public TutoringReservationServiceTests()
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
            .Returns(new Session { Id = "cs_tutoring_123", Url = "https://stripe.test/checkout" });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BusinessSettings:TutoringPackageValidityDays"] = "15",
            })
            .Build();

        _service = new TutoringReservationService(
            _context, _feeCalculator, _checkoutClient, config,
            Substitute.For<ILogger<TutoringReservationService>>());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<TutoringAdvert> SeedTutoringAsync(
        decimal price = 30m, AdvertStatus status = AdvertStatus.ACTIVE,
        string sellerId = SellerId, int maxHours = 10, int? minHours = 1)
    {
        var advert = new TutoringAdvert
        {
            Title = "Cours de maths",
            Description = "desc",
            Price = price,
            Status = status,
            SellerId = sellerId,
            NotificationDate = DateTime.UtcNow,
            SubjectId = 1,
            SchoolGradeId = 1,
            StudyLevel = "Secondaire",
            MaxHours = maxHours,
            MinHours = minHours,
        };
        _context.Services.Add(advert);
        await _context.SaveChangesAsync();
        return advert;
    }

    [Fact]
    public async Task Reserve_PricesServerSide_AndCreatesPendingTransaction_AdvertStaysActive()
    {
        var advert = await SeedTutoringAsync(price: 30m);

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 5, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Url.Should().Be("https://stripe.test/checkout");
        result.Data.OrderNumber.Should().NotBeNullOrWhiteSpace();

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.PENDING);
        transaction.BuyerId.Should().Be(BuyerId);
        transaction.StripeSessionId.Should().Be("cs_tutoring_123");
        transaction.Quantity.Should().Be(5);
        transaction.UnitPrice.Should().Be(30m);
        transaction.PlatformFee.Should().Be(15m);   // 10% of 150
        transaction.Amount.Should().Be(165m);        // 150 + 15
        transaction.PackageExpiresAt.Should().NotBeNull();
        transaction.OrderNumber.Should().Be(result.Data.OrderNumber);

        // A tutoring advert is never paused/sold: multiple students can book it in parallel.
        (await _context.Services.FindAsync(advert.AdvertId))!.Status.Should().Be(AdvertStatus.ACTIVE);
    }

    [Fact]
    public async Task Reserve_ReturnsBadRequest_WhenHoursExceedMax()
    {
        var advert = await SeedTutoringAsync(maxHours: 4);

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 5, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.BadRequest);
        (await _context.Transactions.AnyAsync()).Should().BeFalse();
        await _checkoutClient.DidNotReceive().CreateSessionAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserve_ReturnsBadRequest_WhenHoursBelowMin()
    {
        var advert = await SeedTutoringAsync(minHours: 3);

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 2, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task Reserve_ReturnsNotFound_WhenAdvertMissing()
    {
        var result = await _service.CreateReservationSessionAsync(999, hours: 2, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Reserve_ReturnsConflict_WhenAdvertNotActive()
    {
        var advert = await SeedTutoringAsync(status: AdvertStatus.PAUSED);

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 2, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Reserve_ReturnsConflict_WhenReservingOwnAdvert()
    {
        var advert = await SeedTutoringAsync(sellerId: BuyerId);

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 2, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Reserve_DoesNotTouchDb_WhenStripeFails()
    {
        var advert = await SeedTutoringAsync();
        _checkoutClient.CreateSessionAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns<Session>(_ => throw new Stripe.StripeException("boom"));

        var result = await _service.CreateReservationSessionAsync(advert.AdvertId, hours: 3, BuyerId, BaseUrl);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.InternalError);
        (await _context.Transactions.AnyAsync()).Should().BeFalse();
    }
}
