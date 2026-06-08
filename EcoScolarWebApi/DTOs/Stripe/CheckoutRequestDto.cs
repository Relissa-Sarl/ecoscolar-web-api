using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Stripe;

public class CheckoutRequestDto
{
    [Required]
    public long AdvertId { get; set; }

    [Required]
    public string ShippingAddress { get; set; } = string.Empty;
}
