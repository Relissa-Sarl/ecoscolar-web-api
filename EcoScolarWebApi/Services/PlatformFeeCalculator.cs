using EcoScolarWebApi.Services.Contracts;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Default platform fee = percentage of the amount + an optional fixed part.
/// Configured under <c>BusinessSettings</c> and applied identically to goods and services:
/// <list type="bullet">
///   <item><c>BusinessSettings:PlatformFeePercent</c> (default 5)</item>
///   <item><c>BusinessSettings:PlatformFeeFixed</c> (default 0)</item>
/// </list>
/// </summary>
public class PlatformFeeCalculator : IPlatformFeeCalculator
{
    private const decimal DefaultPercent = 5m;
    private const decimal DefaultFixed = 0m;

    private readonly decimal _percent;
    private readonly decimal _fixed;

    public PlatformFeeCalculator(IConfiguration configuration)
    {
        _percent = configuration.GetValue("BusinessSettings:PlatformFeePercent", DefaultPercent);
        _fixed = configuration.GetValue("BusinessSettings:PlatformFeeFixed", DefaultFixed);
    }

    public decimal CalculateFee(decimal amount)
    {
        if (amount <= 0)
            return 0m;

        var fee = amount * _percent / 100m + _fixed;
        return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
    }
}
