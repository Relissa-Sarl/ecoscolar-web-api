namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Computes the shipping fee for an order.
/// </summary>
public interface IShippingFeeCalculator
{
    /// <summary>
    /// Returns the shipping fee (in CHF) for the given shipping method.
    /// </summary>
    decimal CalculateFee(string? shippingMethod);
}
