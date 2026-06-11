using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Stripe;
using Stripe.V2.Core;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class StripeConnectServiceTests
{
	private readonly UserManager<User> _userManagerMock;
	private readonly IConfiguration _configurationMock;
	private readonly IStripeClientWrapper _stripeClientWrapperMock;
	private readonly StripeConnectService _service;
	private readonly ClaimsPrincipal _principal;

	public StripeConnectServiceTests()
	{
		var store = Substitute.For<IUserStore<User>>();
		_userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
		_configurationMock = Substitute.For<IConfiguration>();
		_stripeClientWrapperMock = Substitute.For<IStripeClientWrapper>();
		
		_configurationMock["Stripe:SecretKey"].Returns("sk_test_mock");
		
		_service = new StripeConnectService(_userManagerMock, _configurationMock, _stripeClientWrapperMock);
		_principal = new ClaimsPrincipal();
	}

	#region CreateOnboardingLinkAsync Tests

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userManagerMock.GetUserAsync(_principal).Returns((User?)null);

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.NotFound);
		result.Errors.Should().Contain("User not found.");
	}

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldReturnBadRequest_WhenUserEmailIsMissing()
	{
		// Arrange
		var user = new User { Id = "user-123", Email = null };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.BadRequest);
		result.Errors.Should().Contain("The user has no email address.");
	}

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldCreateStripeAccountAndUpdateUser_WhenStripeAccountIdIsNull()
	{
		// Arrange
		var user = new User { Id = "user-123", Email = "test@example.com" };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeAccount = new Stripe.V2.Core.Account { Id = "acct_created" };
		_stripeClientWrapperMock.CreateAccountAsync(Arg.Any<Stripe.V2.Core.AccountCreateOptions>())
			.Returns(fakeAccount);

		_userManagerMock.UpdateAsync(user).Returns(IdentityResult.Success);

		var fakeLink = new Stripe.V2.Core.AccountLink { Url = "https://stripe.com/onboard" };
		_stripeClientWrapperMock.CreateAccountLinkAsync(Arg.Any<Stripe.V2.Core.AccountLinkCreateOptions>())
			.Returns(fakeLink);

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.Url.Should().Be("https://stripe.com/onboard");
		user.StripeAccountId.Should().Be("acct_created");
		await _stripeClientWrapperMock.Received(1).CreateAccountAsync(Arg.Any<Stripe.V2.Core.AccountCreateOptions>());
		await _userManagerMock.Received(1).UpdateAsync(user);
		await _stripeClientWrapperMock.Received(1).CreateAccountLinkAsync(Arg.Is<Stripe.V2.Core.AccountLinkCreateOptions>(opts => opts.Account == "acct_created"));
	}

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldReuseStripeAccountId_WhenStripeAccountIdIsAlreadySet()
	{
		// Arrange
		var user = new User { Id = "user-123", Email = "test@example.com", StripeAccountId = "acct_existing" };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeLink = new Stripe.V2.Core.AccountLink { Url = "https://stripe.com/onboard" };
		_stripeClientWrapperMock.CreateAccountLinkAsync(Arg.Any<Stripe.V2.Core.AccountLinkCreateOptions>())
			.Returns(fakeLink);

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.Url.Should().Be("https://stripe.com/onboard");
		user.StripeAccountId.Should().Be("acct_existing");
		await _stripeClientWrapperMock.DidNotReceive().CreateAccountAsync(Arg.Any<Stripe.V2.Core.AccountCreateOptions>());
		await _userManagerMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
		await _stripeClientWrapperMock.Received(1).CreateAccountLinkAsync(Arg.Is<Stripe.V2.Core.AccountLinkCreateOptions>(opts => opts.Account == "acct_existing"));
	}

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldReturnInternalError_WhenUserManagerUpdateFails()
	{
		// Arrange
		var user = new User { Id = "user-123", Email = "test@example.com" };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeAccount = new Stripe.V2.Core.Account { Id = "acct_created" };
		_stripeClientWrapperMock.CreateAccountAsync(Arg.Any<Stripe.V2.Core.AccountCreateOptions>())
			.Returns(fakeAccount);

		var errors = new[] { new IdentityError { Description = "Database lock failure." } };
		_userManagerMock.UpdateAsync(user).Returns(IdentityResult.Failed(errors));

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.InternalError);
		result.Errors.Should().Contain("Database lock failure.");
	}

	[Fact]
	public async Task CreateOnboardingLinkAsync_ShouldReturnInternalError_WhenStripeExceptionThrown()
	{
		// Arrange
		var user = new User { Id = "user-123", Email = "test@example.com", StripeAccountId = "acct_existing" };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var exception = new StripeException("Stripe API down");
		_stripeClientWrapperMock.CreateAccountLinkAsync(Arg.Any<Stripe.V2.Core.AccountLinkCreateOptions>())
			.Returns(Task.FromException<Stripe.V2.Core.AccountLink>(exception));

		// Act
		var result = await _service.CreateOnboardingLinkAsync(_principal, "http://frontend");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.InternalError);
		result.Errors.Should().Contain("Stripe API down");
	}

	#endregion

	#region GetStatusAsync Tests

	[Fact]
	public async Task GetStatusAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userManagerMock.GetUserAsync(_principal).Returns((User?)null);

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.NotFound);
		result.Errors.Should().Contain("User not found.");
	}

	[Fact]
	public async Task GetStatusAsync_ShouldReturnSuccessWithNotOnboarded_WhenStripeAccountIdIsNull()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = null, IsStripeOnboarded = false };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.IsStripeOnboarded.Should().BeFalse();
		result.Data!.StripeAccountId.Should().BeNull();
		await _stripeClientWrapperMock.DidNotReceive().GetAccountAsync(Arg.Any<string>(), Arg.Any<Stripe.V2.Core.AccountGetOptions>());
	}

	[Fact]
	public async Task GetStatusAsync_ShouldReturnSuccessWithOnboarded_WhenUserIsAlreadyMarkedOnboarded()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = "acct_123", IsStripeOnboarded = true };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.IsStripeOnboarded.Should().BeTrue();
		result.Data!.StripeAccountId.Should().Be("acct_123");
		await _stripeClientWrapperMock.DidNotReceive().GetAccountAsync(Arg.Any<string>(), Arg.Any<Stripe.V2.Core.AccountGetOptions>());
	}

	[Fact]
	public async Task GetStatusAsync_ShouldSyncStatusWithStripe_WhenNotOnboardedAndStripeStatusIsActive()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = "acct_123", IsStripeOnboarded = false };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeAccount = new Stripe.V2.Core.Account
		{
			Configuration = new AccountConfiguration
			{
				Recipient = new AccountConfigurationRecipient
				{
					Capabilities = new AccountConfigurationRecipientCapabilities
					{
						StripeBalance = new AccountConfigurationRecipientCapabilitiesStripeBalance
						{
							StripeTransfers = new AccountConfigurationRecipientCapabilitiesStripeBalanceStripeTransfers
							{
								Status = "active"
							}
						}
					}
				}
			}
		};

		_stripeClientWrapperMock.GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>())
			.Returns(fakeAccount);

		_userManagerMock.UpdateAsync(user).Returns(IdentityResult.Success);

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.IsStripeOnboarded.Should().BeTrue();
		result.Data!.StripeAccountId.Should().Be("acct_123");
		user.IsStripeOnboarded.Should().BeTrue();
		await _stripeClientWrapperMock.Received(1).GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>());
		await _userManagerMock.Received(1).UpdateAsync(user);
	}

	[Fact]
	public async Task GetStatusAsync_ShouldKeepStatusNotOnboarded_WhenNotOnboardedAndStripeStatusIsNotActive()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = "acct_123", IsStripeOnboarded = false };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeAccount = new Stripe.V2.Core.Account
		{
			Configuration = new AccountConfiguration
			{
				Recipient = new AccountConfigurationRecipient
				{
					Capabilities = new AccountConfigurationRecipientCapabilities
					{
						StripeBalance = new AccountConfigurationRecipientCapabilitiesStripeBalance
						{
							StripeTransfers = new AccountConfigurationRecipientCapabilitiesStripeBalanceStripeTransfers
							{
								Status = "inactive"
							}
						}
					}
				}
			}
		};

		_stripeClientWrapperMock.GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>())
			.Returns(fakeAccount);

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Data!.IsStripeOnboarded.Should().BeFalse();
		result.Data!.StripeAccountId.Should().Be("acct_123");
		user.IsStripeOnboarded.Should().BeFalse();
		await _stripeClientWrapperMock.Received(1).GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>());
		await _userManagerMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
	}

	[Fact]
	public async Task GetStatusAsync_ShouldReturnInternalError_WhenUserManagerUpdateFails()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = "acct_123", IsStripeOnboarded = false };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var fakeAccount = new Stripe.V2.Core.Account
		{
			Configuration = new AccountConfiguration
			{
				Recipient = new AccountConfigurationRecipient
				{
					Capabilities = new AccountConfigurationRecipientCapabilities
					{
						StripeBalance = new AccountConfigurationRecipientCapabilitiesStripeBalance
						{
							StripeTransfers = new AccountConfigurationRecipientCapabilitiesStripeBalanceStripeTransfers
							{
								Status = "active"
							}
						}
					}
				}
			}
		};

		_stripeClientWrapperMock.GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>())
			.Returns(fakeAccount);

		var errors = new[] { new IdentityError { Description = "Database write error." } };
		_userManagerMock.UpdateAsync(user).Returns(IdentityResult.Failed(errors));

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.InternalError);
		result.Errors.Should().Contain("Database write error.");
	}

	[Fact]
	public async Task GetStatusAsync_ShouldReturnInternalError_WhenStripeExceptionThrown()
	{
		// Arrange
		var user = new User { Id = "user-123", StripeAccountId = "acct_123", IsStripeOnboarded = false };
		_userManagerMock.GetUserAsync(_principal).Returns(user);

		var exception = new StripeException("Connection refused");
		_stripeClientWrapperMock.GetAccountAsync("acct_123", Arg.Any<Stripe.V2.Core.AccountGetOptions>())
			.Returns(Task.FromException<Stripe.V2.Core.Account>(exception));

		// Act
		var result = await _service.GetStatusAsync(_principal);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.InternalError);
		result.Errors.Should().Contain("Connection refused");
	}

	#endregion
}
