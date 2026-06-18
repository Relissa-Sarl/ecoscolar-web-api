using EcoScolarWebApi.Services.Contracts;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Flat shipping fee: hand-to-hand delivery is free, everything else costs a fixed amount.
/// Kept deliberately simple for now (could later depend on weight or destination).
/// </summary>
public class ShippingFeeCalculator : IShippingFeeCalculator
{
    private const decimal FixedFee = 2m;

    public decimal CalculateFee(string? shippingMethod)
        => shippingMethod == "handToHand" ? 0m : FixedFee;
}
