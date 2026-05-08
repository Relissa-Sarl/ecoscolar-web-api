namespace EcoscolarWebApi.Utils.DTOs.Stripe
{
    public class CheckoutRequestDto
    {
        public int ProductId { get; set; }
        public double ProductPrice { get; set; }
    }
}
