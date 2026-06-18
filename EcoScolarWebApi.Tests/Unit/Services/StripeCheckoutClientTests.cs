using Xunit;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Stripe;
using Stripe.Checkout;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class StripeCheckoutClientTests
{
    [Fact]
    public async Task CreateSessionAsync_WithoutApiKey_ThrowsStripeException()
    {
        // Arrange
        var client = new StripeCheckoutClient();
        var options = new SessionCreateOptions();

        // Act
        Func<Task> act = async () => await client.CreateSessionAsync(options);

        // Assert
        await act.Should().ThrowAsync<StripeException>()
            .WithMessage("*");
    }
}
