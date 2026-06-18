using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

/// <summary>
/// The controller is a thin delegator to <see cref="IPaymentService"/>; the checkout business
/// logic (server-side pricing, pausing, idempotent webhook) is covered by PaymentServiceTests.
/// These tests only verify the HTTP mapping.
/// </summary>
public class PaymentsControllerTests
{
    private const string BuyerId = "buyer-1";

    private readonly IPaymentService _paymentService;
    private readonly UserManager<User> _userManager;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _paymentService = Substitute.For<IPaymentService>();

        var store = Substitute.For<IUserStore<User>>();
        _userManager = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
        _userManager.GetUserId(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(BuyerId);

        var config = Substitute.For<IConfiguration>();

        _controller = new PaymentsController(config, _paymentService, _userManager, Substitute.For<ILogger<PaymentsController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { Scheme = "https", Host = new HostString("localhost", 5001) }
                }
            }
        };
    }

    private CheckoutRequestDto Request() => new() { ProductIds = new List<long> { 1L }, ShippingMethod = "handToHand" };

    [Fact]
    public async Task Checkout_ReturnsOk_WhenServiceSucceeds()
    {
        _paymentService.CreateCheckoutSessionAsync(Arg.Any<CheckoutRequestDto>(), BuyerId, Arg.Any<string>())
            .Returns(Result<CheckoutSessionResultDto>.Success(new CheckoutSessionResultDto("https://stripe.test/checkout", "ECO-1")));

        var result = await _controller.Checkout(Request());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Checkout_ReturnsUnauthorized_WhenNoAuthenticatedUser()
    {
        _userManager.GetUserId(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns((string?)null);

        var result = await _controller.Checkout(Request());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Checkout_ReturnsNotFound_WhenServiceReturnsNotFound()
    {
        _paymentService.CreateCheckoutSessionAsync(Arg.Any<CheckoutRequestDto>(), BuyerId, Arg.Any<string>())
            .Returns(Result<CheckoutSessionResultDto>.Failure("missing", ErrorType.NotFound));

        var result = await _controller.Checkout(Request());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Checkout_ReturnsConflict_WhenServiceReturnsConflict()
    {
        _paymentService.CreateCheckoutSessionAsync(Arg.Any<CheckoutRequestDto>(), BuyerId, Arg.Any<string>())
            .Returns(Result<CheckoutSessionResultDto>.Failure("unavailable", ErrorType.Conflict));

        var result = await _controller.Checkout(Request());

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Checkout_ReturnsBadGateway_WhenServiceReturnsInternalError()
    {
        _paymentService.CreateCheckoutSessionAsync(Arg.Any<CheckoutRequestDto>(), BuyerId, Arg.Any<string>())
            .Returns(Result<CheckoutSessionResultDto>.Failure("stripe down", ErrorType.InternalError));

        var result = await _controller.Checkout(Request());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }
}
