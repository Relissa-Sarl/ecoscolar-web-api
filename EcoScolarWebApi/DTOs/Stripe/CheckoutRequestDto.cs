using System.Collections.Generic;

namespace EcoScolarWebApi.DTOs.Stripe;

public class CheckoutRequestDto
{
    public int ProductId { get; set; }
    public List<long>? ProductIds { get; set; }
    public double ProductPrice { get; set; }
}
