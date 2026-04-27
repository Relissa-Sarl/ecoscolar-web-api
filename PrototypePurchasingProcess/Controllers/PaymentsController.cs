using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace PrototypePurchasingProcess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PaymentsController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("test-checkout")]
        public async Task<IActionResult> TestCheckout()
        {
            // Define the Stripe Checkout options for a hardcoded 1 CHF test
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            // Stripe expects the amount in the smallest currency unit. 
                            // For CHF, 1 Franc = 100 centimes/rappen.
                            UnitAmount = 1000,
                            Currency = "chf",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Test Integration Purchase (1 CHF)",
                                Description = "This is a fake purchase to test the Stripe integration."
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                // Using generic example URLs so you don't even need your frontend fully built yet
                SuccessUrl = "https://example.com/success",
                CancelUrl = "https://example.com/cancel",
            };

            var service = new SessionService();
            var client = new Stripe.StripeClient("<API-KEY>");
            Session session = await service.CreateAsync(options);

            // Return the URL. You can copy-paste this URL directly into your browser to test it.
            return Ok(new { url = session.Url });
        }

    }
}
