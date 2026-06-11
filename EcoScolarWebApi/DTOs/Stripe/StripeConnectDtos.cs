namespace EcoScolarWebApi.DTOs.Stripe;

/// <summary>
/// Response returned when generating a Stripe Connect onboarding link for the current user.
/// </summary>
/// <param name="Url">URL of the Stripe-hosted onboarding page to redirect the seller to.</param>
public record StripeOnboardingResponseDto(string Url);

/// <summary>
/// Current Stripe Connect status of the authenticated user.
/// </summary>
/// <param name="IsStripeOnboarded">True once the seller completed the Stripe onboarding and can receive transfers.</param>
/// <param name="StripeAccountId">The Stripe Connect account ID, or null if no account was created yet.</param>
public record StripeStatusDto(bool IsStripeOnboarded, string? StripeAccountId);
