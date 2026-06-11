using System.Threading.Tasks;
using EcoScolarWebApi.Services.Contracts;
using Stripe;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Concrete implementation of IStripeClientWrapper wrapping the StripeClient class.
/// </summary>
public class StripeClientWrapper(IStripeClient client) : IStripeClientWrapper
{
	public Task<Stripe.V2.Core.Account> CreateAccountAsync(Stripe.V2.Core.AccountCreateOptions options)
	{
		// Since V2 property is on StripeClient but not IStripeClient, we cast to StripeClient
		var stripeClient = (StripeClient)client;
		return stripeClient.V2.Core.Accounts.CreateAsync(options);
	}

	public Task<Stripe.V2.Core.AccountLink> CreateAccountLinkAsync(Stripe.V2.Core.AccountLinkCreateOptions options)
	{
		var stripeClient = (StripeClient)client;
		return stripeClient.V2.Core.AccountLinks.CreateAsync(options);
	}

	public Task<Stripe.V2.Core.Account> GetAccountAsync(string accountId, Stripe.V2.Core.AccountGetOptions options)
	{
		var stripeClient = (StripeClient)client;
		return stripeClient.V2.Core.Accounts.GetAsync(accountId, options);
	}
}
