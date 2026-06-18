using Xunit;
using EcoScolarWebApi.Services;
using FluentAssertions;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class ShippingFeeCalculatorTests
{
    private readonly ShippingFeeCalculator _calculator;

    public ShippingFeeCalculatorTests()
    {
        _calculator = new ShippingFeeCalculator();
    }

    [Fact]
    public void CalculateFee_WhenMethodIsHandToHand_ReturnsZero()
    {
        // Act
        var result = _calculator.CalculateFee("handToHand");

        // Assert
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData("postal")]
    [InlineData("courier")]
    [InlineData(null)]
    [InlineData("")]
    public void CalculateFee_WhenMethodIsNotHandToHand_ReturnsFixedFee(string? method)
    {
        // Act
        var result = _calculator.CalculateFee(method);

        // Assert
        result.Should().Be(2m);
    }
}
