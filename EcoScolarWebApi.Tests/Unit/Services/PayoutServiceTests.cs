using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stripe;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class PayoutServiceTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly IStripeTransferClient _transferClient;
    private readonly SellerPayoutService _service;

    public PayoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        _transferClient = Substitute.For<IStripeTransferClient>();
        _transferClient.CreateTransferAsync(Arg.Any<TransferCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Transfer { Id = "tr_123" });

        _service = new SellerPayoutService(_context, _transferClient, Substitute.For<ILogger<SellerPayoutService>>());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Transaction> SeedTransactionAsync(
        decimal amount = 33m, decimal fee = 3m, string? stripeAccountId = "acct_seller", string? transferId = null)
    {
        var seller = new User
        {
            Id = $"seller-{Guid.NewGuid():N}",
            UserName = $"seller-{Guid.NewGuid():N}@test.ch",
            Email = $"seller-{Guid.NewGuid():N}@test.ch",
            StripeAccountId = stripeAccountId,
        };
        _context.Users.Add(seller);

        var advert = new PhysicalItem
        {
            Title = "A book",
            Description = "desc",
            Price = 30m,
            Status = AdvertStatus.SOLD,
            SellerId = seller.Id,
            NotificationDate = DateTime.UtcNow,
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            AdvertId = advert.AdvertId,
            BuyerId = "buyer-1",
            Status = TransactionStatus.COMPLETED,
            OrderNumber = "ECO-1",
            Amount = amount,
            PlatformFee = fee,
            StripeTransferId = transferId,
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return await _context.Transactions
            .Include(t => t.Advert).ThenInclude(a => a.Seller)
            .FirstAsync(t => t.TransactionId == transaction.TransactionId);
    }

    [Fact]
    public async Task ReleaseFunds_TransfersNetAmount_AndStoresTransferId()
    {
        var transaction = await SeedTransactionAsync(amount: 33m, fee: 3m);

        var result = await _service.ReleaseFundsAsync(transaction);

        result.IsSuccess.Should().BeTrue();
        transaction.StripeTransferId.Should().Be("tr_123");

        // Seller receives amount net of fee = 30 CHF = 3000 cents; transfer group = order number.
        await _transferClient.Received(1).CreateTransferAsync(
            Arg.Is<TransferCreateOptions>(o =>
                o.Amount == 3000
                && o.Currency == "chf"
                && o.Destination == "acct_seller"
                && o.TransferGroup == "ECO-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseFunds_IsIdempotent_WhenAlreadyTransferred()
    {
        var transaction = await SeedTransactionAsync(transferId: "tr_existing");

        var result = await _service.ReleaseFundsAsync(transaction);

        result.IsSuccess.Should().BeTrue();
        transaction.StripeTransferId.Should().Be("tr_existing");
        await _transferClient.DidNotReceive().CreateTransferAsync(Arg.Any<TransferCreateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseFunds_ReturnsConflict_WhenSellerHasNoConnectedAccount()
    {
        var transaction = await SeedTransactionAsync(stripeAccountId: null);

        var result = await _service.ReleaseFundsAsync(transaction);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        transaction.StripeTransferId.Should().BeNull();
        await _transferClient.DidNotReceive().CreateTransferAsync(Arg.Any<TransferCreateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseFunds_ReturnsFailure_OnStripeError_AndDoesNotStoreTransferId()
    {
        var transaction = await SeedTransactionAsync();
        _transferClient.CreateTransferAsync(Arg.Any<TransferCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns<Transfer>(_ => throw new StripeException("boom"));

        var result = await _service.ReleaseFundsAsync(transaction);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.InternalError);
        transaction.StripeTransferId.Should().BeNull();
    }
}
