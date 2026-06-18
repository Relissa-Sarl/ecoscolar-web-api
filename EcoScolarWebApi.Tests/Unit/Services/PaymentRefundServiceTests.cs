using Xunit;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stripe;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class PaymentRefundServiceTests
{
    private readonly IStripeRefundClient _refundClient;
    private readonly PaymentRefundService _service;

    public PaymentRefundServiceTests()
    {
        _refundClient = Substitute.For<IStripeRefundClient>();
        _refundClient.CreateRefundAsync(Arg.Any<RefundCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Refund { Id = "re_123" });

        _service = new PaymentRefundService(_refundClient, Substitute.For<ILogger<PaymentRefundService>>());
    }

    [Fact]
    public async Task RefundAsync_RefundsThePaymentIntent_WhenPresent()
    {
        var transaction = new Transaction { TransactionId = 1, StripePaymentIntentId = "pi_1" };

        var result = await _service.RefundAsync(transaction);

        result.IsSuccess.Should().BeTrue();
        await _refundClient.Received(1).CreateRefundAsync(
            Arg.Is<RefundCreateOptions>(o => o.PaymentIntent == "pi_1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundAsync_ReturnsConflict_WhenNoPaymentIntent()
    {
        var transaction = new Transaction { TransactionId = 1, StripePaymentIntentId = null };

        var result = await _service.RefundAsync(transaction);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        await _refundClient.DidNotReceive().CreateRefundAsync(Arg.Any<RefundCreateOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundAsync_ReturnsFailure_OnStripeError()
    {
        var transaction = new Transaction { TransactionId = 1, StripePaymentIntentId = "pi_1" };
        _refundClient.CreateRefundAsync(Arg.Any<RefundCreateOptions>(), Arg.Any<CancellationToken>())
            .Returns<Refund>(_ => throw new StripeException("boom"));

        var result = await _service.RefundAsync(transaction);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.InternalError);
    }
}
