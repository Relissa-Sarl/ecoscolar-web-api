using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Stripe;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Contract for the Stripe Connect (Express) seller onboarding service.
/// </summary>
public interface IStripeConnectService
{
	/// <summary>
	/// Creates the Stripe Connect account for the current user if it does not exist yet,
	/// then generates a Stripe-hosted onboarding link for it.
	/// </summary>
	/// <param name="principal">The authenticated user principal.</param>
	/// <param name="frontendBaseUrl">Base URL of the frontend, used to build the onboarding return/refresh URLs.</param>
	/// <returns>The URL the seller must be redirected to in order to complete the onboarding.</returns>
	Task<Result<StripeOnboardingResponseDto>> CreateOnboardingLinkAsync(ClaimsPrincipal principal, string frontendBaseUrl);

	/// <summary>
	/// Returns the Stripe Connect status of the current user, synchronizing the
	/// onboarding flag with Stripe when the account is not yet marked as onboarded.
	/// </summary>
	/// <param name="principal">The authenticated user principal.</param>
	/// <returns>The Stripe account ID and whether the onboarding is complete.</returns>
	Task<Result<StripeStatusDto>> GetStatusAsync(ClaimsPrincipal principal);
}
