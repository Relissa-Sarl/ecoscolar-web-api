using System.Collections.Generic;

namespace EcoScolarWebApi.DTOs.Stripe;

/// <summary>
/// Checkout request. Carries advert identifiers only — never a price.
/// The server reads <c>Advert.Price</c> and computes the amount and platform fee.
/// </summary>
public class CheckoutRequestDto
{
    public int ProductId { get; set; }
    public List<long>? ProductIds { get; set; }
    public string? ShippingMethod { get; set; }
}
