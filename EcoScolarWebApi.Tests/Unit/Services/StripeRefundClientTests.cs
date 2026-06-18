using Xunit;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Stripe;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class StripeRefundClientTests
{
    [Fact]
    public async Task CreateRefundAsync_WithoutApiKey_ThrowsStripeException()
    {
        // Arrange
        var client = new StripeRefundClient();
        var options = new RefundCreateOptions();

        // Act
        Func<Task> act = async () => await client.CreateRefundAsync(options);

        // Assert
        await act.Should().ThrowAsync<StripeException>()
            .WithMessage("*");
    }
}
