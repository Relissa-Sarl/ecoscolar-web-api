using Xunit;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class TutoringEscrowBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCallProcessDueTransactionsAsync_AndThenWait()
    {
        // Arrange
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        var serviceScopeFactoryMock = Substitute.For<IServiceScopeFactory>();
        var serviceScopeMock = Substitute.For<IServiceScope>();
        var scopedServiceProviderMock = Substitute.For<IServiceProvider>();

        var processorMock = Substitute.For<ITutoringEscrowProcessor>();
        var loggerMock = Substitute.For<ILogger<TutoringEscrowBackgroundService>>();

        serviceProviderMock.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactoryMock);
        serviceScopeFactoryMock.CreateScope().Returns(serviceScopeMock);
        serviceScopeMock.ServiceProvider.Returns(scopedServiceProviderMock);

        scopedServiceProviderMock.GetService(typeof(ITutoringEscrowProcessor)).Returns(processorMock);

        var backgroundService = new TutoringEscrowBackgroundService(serviceProviderMock, loggerMock);

        var cts = new CancellationTokenSource();

        // When the processor is called, cancel the token to stop the background service loop
        processorMock
            .When(x => x.ProcessDueTransactionsAsync(Arg.Any<CancellationToken>()))
            .Do(_ => cts.Cancel());

        // Act
        await backgroundService.StartAsync(cts.Token);

        // Wait briefly to allow loop to run before it cancels itself
        try
        {
            await backgroundService.ExecuteTask!;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert
        await processorMock.Received(1).ProcessDueTransactionsAsync(Arg.Any<CancellationToken>());
    }
}
