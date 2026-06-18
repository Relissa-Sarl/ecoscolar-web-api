namespace EcoScolarWebApi.DTOs.Stripe;

/// <summary>
/// The Stripe session URL and the order number grouping this cart's transactions.
/// </summary>
public record CheckoutSessionResultDto(string Url, string OrderNumber);
