namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Computes the platform commission for a purchase.
/// The rule is intentionally identical for physical goods and services (tutoring).
/// </summary>
public interface IPlatformFeeCalculator
{
    /// <summary>
    /// Returns the platform fee for a given gross amount (in CHF).
    /// </summary>
    /// <param name="amount">Gross amount charged to the buyer (Quantity * UnitPrice).</param>
    /// <returns>The platform fee, rounded to 2 decimals.</returns>
    decimal CalculateFee(decimal amount);
}
