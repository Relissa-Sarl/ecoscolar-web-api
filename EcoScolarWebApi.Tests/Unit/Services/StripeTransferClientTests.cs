using Xunit;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Stripe;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class StripeTransferClientTests
{
    [Fact]
    public async Task CreateTransferAsync_WithoutApiKey_ThrowsStripeException()
    {
        // Arrange
        var client = new StripeTransferClient();
        var options = new TransferCreateOptions();

        // Act
        Func<Task> act = async () => await client.CreateTransferAsync(options);

        // Assert
        await act.Should().ThrowAsync<StripeException>()
            .WithMessage("*");
    }
}
