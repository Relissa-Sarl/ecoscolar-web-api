using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Stripe.V2.Core;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Stripe Connect (Express) seller onboarding service.
/// Adapted from the purchasing process prototype: the account creation is now tied to the
/// authenticated user and the resulting account ID is persisted on the User entity.
/// </summary>
public class StripeConnectService(UserManager<User> userManager, IConfiguration config) : IStripeConnectService
{
	public async Task<Result<StripeOnboardingResponseDto>> CreateOnboardingLinkAsync(ClaimsPrincipal principal, string frontendBaseUrl)
	{
		var user = await userManager.GetUserAsync(principal);
		if (user == null)
			return Result<StripeOnboardingResponseDto>.Failure("User not found.", ErrorType.NotFound);

		if (string.IsNullOrWhiteSpace(user.Email))
			return Result<StripeOnboardingResponseDto>.Failure("The user has no email address.", ErrorType.BadRequest);

		try
		{
			var client = CreateClient();

			// Create the v2 Connect account once, then reuse it for subsequent onboarding attempts
			if (string.IsNullOrEmpty(user.StripeAccountId))
			{
				var account = await client.V2.Core.Accounts.CreateAsync(BuildAccountCreateOptions(user));

				user.StripeAccountId = account.Id;
				var updateResult = await userManager.UpdateAsync(user);
				if (!updateResult.Succeeded)
					return Result<StripeOnboardingResponseDto>.Failure(
						updateResult.Errors.Select(e => e.Description), ErrorType.InternalError);
			}

			var linkOptions = new Stripe.V2.Core.AccountLinkCreateOptions
			{
				Account = user.StripeAccountId,
				UseCase = new AccountLinkCreateUseCaseOptions
				{
					Type = "account_onboarding",
					AccountOnboarding = new AccountLinkCreateUseCaseAccountOnboardingOptions
					{
						Configurations = ["recipient"],
						RefreshUrl = $"{frontendBaseUrl}/me/profile?stripe=refresh",
						ReturnUrl = $"{frontendBaseUrl}/me/profile?stripe=return",
					},
				},
			};

			var accountLink = await client.V2.Core.AccountLinks.CreateAsync(linkOptions);

			return Result<StripeOnboardingResponseDto>.Success(new StripeOnboardingResponseDto(accountLink.Url));
		}
		catch (StripeException e)
		{
			return Result<StripeOnboardingResponseDto>.Failure(
				e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
		}
	}

	public async Task<Result<StripeStatusDto>> GetStatusAsync(ClaimsPrincipal principal)
	{
		var user = await userManager.GetUserAsync(principal);
		if (user == null)
			return Result<StripeStatusDto>.Failure("User not found.", ErrorType.NotFound);

		// No Stripe account yet: nothing to synchronize
		if (string.IsNullOrEmpty(user.StripeAccountId))
			return Result<StripeStatusDto>.Success(new StripeStatusDto(false, null));

		// Once onboarded, the seller stays onboarded: no need to call Stripe again
		if (user.IsStripeOnboarded)
			return Result<StripeStatusDto>.Success(new StripeStatusDto(true, user.StripeAccountId));

		try
		{
			var client = CreateClient();
			var account = await client.V2.Core.Accounts.GetAsync(user.StripeAccountId, new Stripe.V2.Core.AccountGetOptions
			{
				Include = ["configuration.recipient"],
			});

			var transfersStatus = account.Configuration?.Recipient?.Capabilities?.StripeBalance?.StripeTransfers?.Status;
			if (transfersStatus == "active")
			{
				user.IsStripeOnboarded = true;
				var updateResult = await userManager.UpdateAsync(user);
				if (!updateResult.Succeeded)
					return Result<StripeStatusDto>.Failure(
						updateResult.Errors.Select(e => e.Description), ErrorType.InternalError);
			}

			return Result<StripeStatusDto>.Success(new StripeStatusDto(user.IsStripeOnboarded, user.StripeAccountId));
		}
		catch (StripeException e)
		{
			return Result<StripeStatusDto>.Failure(
				e.StripeError?.Message ?? e.Message, ErrorType.InternalError);
		}
	}

	private StripeClient CreateClient() => new StripeClient(config["Stripe:SecretKey"]);

	/// <summary>
	/// Builds the creation options of a v2 Connect account for an individual seller in Switzerland,
	/// configured as a recipient able to receive transfers (escrow payout flow).
	/// </summary>
	private static Stripe.V2.Core.AccountCreateOptions BuildAccountCreateOptions(User user) => new()
	{
		ContactEmail = user.Email,
		DisplayName = user.Nickname ?? user.Email,
		Identity = new AccountCreateIdentityOptions
		{
			Country = "CH",
			EntityType = "individual",
		},
		Configuration = new AccountCreateConfigurationOptions
		{
			Recipient = new AccountCreateConfigurationRecipientOptions
			{
				Capabilities = new AccountCreateConfigurationRecipientCapabilitiesOptions
				{
					StripeBalance = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceOptions
					{
						StripeTransfers = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceStripeTransfersOptions
						{
							Requested = true,
						},
					},
				},
			},
		},
		Defaults = new AccountCreateDefaultsOptions
		{
			Responsibilities = new AccountCreateDefaultsResponsibilitiesOptions
			{
				FeesCollector = "application",
				LossesCollector = "application",
			},
		},
		Dashboard = "express",
		Include =
		[
			"configuration.recipient",
			"requirements",
		],
	};
}
