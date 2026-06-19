using EcoScolarWebApi.Services.Contracts;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Daily background job that drives the tutoring escrow (Étape G): delegates to
/// <see cref="ITutoringEscrowProcessor"/>. Mirrors <see cref="AutoConfirmReceiptService"/>.
/// </summary>
public class TutoringEscrowBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<TutoringEscrowBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TutoringEscrowBackgroundService running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITutoringEscrowProcessor>();
                await processor.ProcessDueTransactionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing TutoringEscrowBackgroundService.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
