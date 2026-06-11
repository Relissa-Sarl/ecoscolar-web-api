using System.Threading.Tasks;

namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Wrapper interface for Stripe V2 API client calls to allow unit testing of StripeConnectService.
/// </summary>
public interface IStripeClientWrapper
{
	Task<Stripe.V2.Core.Account> CreateAccountAsync(Stripe.V2.Core.AccountCreateOptions options);
	Task<Stripe.V2.Core.AccountLink> CreateAccountLinkAsync(Stripe.V2.Core.AccountLinkCreateOptions options);
	Task<Stripe.V2.Core.Account> GetAccountAsync(string accountId, Stripe.V2.Core.AccountGetOptions options);
}
