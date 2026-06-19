using Xunit;
using System.Globalization;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class PlatformFeeCalculatorTests
{
    private static PlatformFeeCalculator CreateCalculator(decimal? percent = null, decimal? fixedFee = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(BuildSettings(percent, fixedFee))
            .Build();
        return new PlatformFeeCalculator(config);
    }

    private static Dictionary<string, string?> BuildSettings(decimal? percent, decimal? fixedFee)
    {
        var dict = new Dictionary<string, string?>();
        if (percent.HasValue)
            dict["BusinessSettings:PlatformFeePercent"] = percent.Value.ToString(CultureInfo.InvariantCulture);
        if (fixedFee.HasValue)
            dict["BusinessSettings:PlatformFeeFixed"] = fixedFee.Value.ToString(CultureInfo.InvariantCulture);
        return dict;
    }

    #region Default configuration (5% + 0 fixed)

    [Fact]
    public void CalculateFee_ShouldReturn5Percent_WhenUsingDefaultConfig()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(100m);

        // Assert
        fee.Should().Be(5.00m);
    }

    [Fact]
    public void CalculateFee_ShouldReturn2Point50_For50Amount()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(50m);

        // Assert
        fee.Should().Be(2.50m);
    }

    [Fact]
    public void CalculateFee_ShouldReturn0Point05_For1Amount()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(1m);

        // Assert
        fee.Should().Be(0.05m);
    }

    #endregion

    #region Edge cases: zero, negative, very large

    [Fact]
    public void CalculateFee_ShouldReturnZero_WhenAmountIsZero()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(0m);

        // Assert
        fee.Should().Be(0m);
    }

    [Fact]
    public void CalculateFee_ShouldReturnZero_WhenAmountIsNegative()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(-50m);

        // Assert
        fee.Should().Be(0m);
    }

    [Fact]
    public void CalculateFee_ShouldHandleVeryLargeAmount()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(999_999m);

        // Assert
        fee.Should().Be(49_999.95m);
    }

    [Fact]
    public void CalculateFee_ShouldHandleVerySmallPositiveAmount()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(0.01m);

        // Assert
        fee.Should().Be(0.00m); // 0.01 * 5 / 100 = 0.0005, rounds to 0.00
    }

    #endregion

    #region Rounding

    [Fact]
    public void CalculateFee_ShouldRoundAwayFromZero_WhenMidpoint()
    {
        // Arrange — 5% of 33.33 = 1.6665, should round to 1.67
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(33.33m);

        // Assert
        fee.Should().Be(1.67m);
    }

    [Fact]
    public void CalculateFee_ShouldRoundToTwoDecimals()
    {
        // Arrange — 5% of 7.77 = 0.3885, should round to 0.39
        var calculator = CreateCalculator();

        // Act
        var fee = calculator.CalculateFee(7.77m);

        // Assert
        fee.Should().Be(0.39m);
    }

    #endregion

    #region Custom configuration

    [Fact]
    public void CalculateFee_ShouldUseCustomPercent()
    {
        // Arrange — 10%
        var calculator = CreateCalculator(percent: 10m);

        // Act
        var fee = calculator.CalculateFee(100m);

        // Assert
        fee.Should().Be(10.00m);
    }

    [Fact]
    public void CalculateFee_ShouldUseFixedFee()
    {
        // Arrange — 5% + 1.00 fixed
        var calculator = CreateCalculator(fixedFee: 1.00m);

        // Act
        var fee = calculator.CalculateFee(100m);

        // Assert
        fee.Should().Be(6.00m); // 5 + 1
    }

    [Fact]
    public void CalculateFee_ShouldCombinePercentAndFixed()
    {
        // Arrange — 3% + 0.50 fixed
        var calculator = CreateCalculator(percent: 3m, fixedFee: 0.50m);

        // Act
        var fee = calculator.CalculateFee(200m);

        // Assert
        fee.Should().Be(6.50m); // 200*3/100 + 0.50 = 6 + 0.50
    }

    [Fact]
    public void CalculateFee_ShouldReturnOnlyFixed_WhenPercentIsZero()
    {
        // Arrange — 0% + 2.00 fixed
        var calculator = CreateCalculator(percent: 0m, fixedFee: 2.00m);

        // Act
        var fee = calculator.CalculateFee(100m);

        // Assert
        fee.Should().Be(2.00m);
    }

    [Fact]
    public void CalculateFee_ShouldReturnZero_WhenAmountNegative_EvenWithFixed()
    {
        // Arrange — fixed fee should not apply for negative amounts
        var calculator = CreateCalculator(fixedFee: 5.00m);

        // Act
        var fee = calculator.CalculateFee(-10m);

        // Assert
        fee.Should().Be(0m);
    }

    #endregion
}
