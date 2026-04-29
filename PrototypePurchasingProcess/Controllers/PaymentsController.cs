using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

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

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            double price = request.ProductPrice * 100;

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
                            UnitAmount = (long)price,
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
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    // Un ID unique de ta base de données pour lier le paiement au futur virement
                    TransferGroup = "COMMANDE_ID_789",
                },

                // Using generic example URLs so you don't even need your frontend fully built yet
                SuccessUrl = "http://localhost:3000/success",
                CancelUrl = "http://localhost:3000/denied",
            };



            var service = new SessionService();
            var client = new Stripe.StripeClient("<API-KEY>");
            Session session = await service.CreateAsync(options);

            // Return the URL. You can copy-paste this URL directly into your browser to test it.
            return Ok(new { url = session.Url });
        }

        [HttpPost("create-transfer")] // Adjust route as needed based on your controller's base route
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequest request)
        {
            try
            {
                var options = new TransferCreateOptions
                {
                    Amount = request.Amount,
                    Currency = "chf", // Remember: you can make this a property in your model too if you want it dynamic!
                    Destination = request.ConnectedAccountId,
                    TransferGroup = request.TransferGroup,
                };

                var transferService = new TransferService();
                Transfer transfer = await transferService.CreateAsync(options);

                return Ok(new { transferId = transfer.Id });
            }
            catch (StripeException e)
            {
                // It's always a good idea to catch Stripe exceptions in controllers
                // so you don't crash the app and instead return a clean 400 Bad Request
                return BadRequest(new { error = e.StripeError.Message });
            }
        }
    }
    public class TransferRequest
    {
        public long Amount { get; set; }
        public string TransferGroup { get; set; }
        public string ConnectedAccountId { get; set; }
    }

    public class CheckoutRequest
    {
        public int ProductId { get; set; }

        public double ProductPrice { get; set; }
    }
}
