using Asp.Versioning;
using EcoScolarWebApi.DTOs.Stripe;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.V2.Core;

namespace EcoScolarWebApi.Controllers
{
	[ApiVersion("1.0")]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _config;        // Configuration to access Stripe secret key

        /// <summary>
        /// PaymentsController constructor
        /// Takes the configuration as a parameter to access the Stripe secret key
        /// 
        /// Url: POST /api/v1/payments/checkout
        /// </summary>
        /// <param name="config">The configuration object containing the Stripe secret key</param>
        public PaymentsController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates a Stripe Checkout session for a given product price 
        /// and returns the session URL to the client
        /// 
        /// Url: POST /api/v1/payments/checkout
        /// </summary>
        /// <param name="request">The checkout request containing product information</param>
        /// <returns>The session URL</returns>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            double price = request.ProductPrice * 100;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            // 1 franc = 100 cents
                            UnitAmount = (long)price,
                            Currency = "chf",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Test Integration Purchase ({price} CHF)",
                                Description = "Fake purchase to validate the process for the prototype"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    TransferGroup = "COMMANDE_ID_789",
                },

                SuccessUrl = "http://localhost:3001/success",
                CancelUrl = "http://localhost:3001/denied",
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }

        /// <summary>
        /// Creates a transfer to a connected account using the Stripe API
        /// 
        /// Url: POST /api/v1/payments/create-transfer
        /// </summary>
        /// <param name="request">The transfer request containing transfer information</param>
        /// <returns>The transfer ID</returns>
        [HttpPost("create-transfer")]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequestDto request)
        {
            try
            {
                var options = new TransferCreateOptions
                {
                    Amount = request.Amount,
                    Currency = "chf",
                    Destination = request.ConnectedAccountId,
                    TransferGroup = request.TransferGroup,
                };

                var transferService = new TransferService();
                Transfer transfer = await transferService.CreateAsync(options);

                return Ok(new { transferId = transfer.Id });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.StripeError.Message });
            }
        }

        /// <summary>
        /// Creates a Stripe Connect account for an individual in Switzerland using the Stripe API v2
        ///     
        /// Url: POST /api/v1/payments/create-connect-account
        /// </summary>
        /// <param name="request">The request containing account information</param>
        /// <returns>The created account ID</returns>
        [HttpPost("create-connect-account")]
        public IActionResult CreateConnectAccount([FromBody] ConnectAccountRequestDto request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                Console.WriteLine("Email is required");
                return BadRequest(new { error = "Email is required." });
            }

            try
            {
                // Create v2 Connect account for an individual in Switzerland
                var options = new Stripe.V2.Core.AccountCreateOptions
                {
                    ContactEmail = request.Email,
                    DisplayName = request.Email,
                    Identity = new AccountCreateIdentityOptions
                    {
                        Country = "CH", // Changed to Switzerland
                        EntityType = "individual", // Changed to individual (particular)
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
                    Include = new List<string>
                    {
                        "configuration.recipient",
                        "requirements",
                    },
                };

                var secretKey = _config["Stripe:SecretKey"];
                var client = new StripeClient(secretKey);

                var service = client.V2.Core.Accounts;
                Stripe.V2.Core.Account account = service.Create(options);

                return Ok(new { accountId = account.Id });
            }
            catch (StripeException e)
            {
                // Catching StripeException specifically can be useful for debugging
                return StatusCode(500, new { error = e.StripeError.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message });
            }
        }

        /// <summary>
        /// Creates an account link for onboarding a connected account using the Stripe API v2
        /// 
        /// Url: POST /api/v1/payments/create-account-link
        /// </summary>
        /// <param name="request">The request containing account link information</param>
        /// <returns>The created account link URL</returns>
        [HttpPost("create-account-link")]
        public IActionResult CreateAccountLink([FromBody] AccountLinkRequestDto request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request?.AccountId))
            {
                return BadRequest(new { error = "Account ID is required." });
            }

            try
            {
                var secretKey = _config["Stripe:SecretKey"];
                var client = new StripeClient(secretKey);
                var service = client.V2.Core.AccountLinks;

                var options = new Stripe.V2.Core.AccountLinkCreateOptions
                {
                    Account = request.AccountId,
                    UseCase = new Stripe.V2.Core.AccountLinkCreateUseCaseOptions
                    {
                        Type = "account_onboarding",
                        AccountOnboarding = new Stripe.V2.Core.AccountLinkCreateUseCaseAccountOnboardingOptions
                        {
                            Configurations = new List<string> { "recipient" },
                            // Note: You should replace these example URLs with your actual front-end URLs
                            RefreshUrl = "http://localhost:3001/home",
                            ReturnUrl = $"http://localhost:3001/home?accountId={request.AccountId}",
                        },
                    },
                };

                var accountLink = service.Create(options);

                return Ok(new { url = accountLink.Url });
            }
            catch (StripeException e)
            {
                // Catching StripeException specifically can be useful for debugging
                return StatusCode(500, new { error = e.StripeError.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message });
            }
        }
    }
}
