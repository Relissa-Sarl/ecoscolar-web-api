using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EcoScolarWebApi.Services;

public class AutoReleaseFundsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoReleaseFundsService> _logger;

    public AutoReleaseFundsService(IServiceProvider serviceProvider, ILogger<AutoReleaseFundsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoReleaseFundsService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAutoReleasesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing auto releases.");
            }

            // Run every day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }

        _logger.LogInformation("AutoReleaseFundsService is stopping.");
    }

    private async Task ProcessAutoReleasesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        var transactionsToComplete = await context.Transactions
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .Where(t => t.Status == TransactionStatus.SHIPPED && t.ShippedDate <= cutoffDate)
            .ToListAsync(cancellationToken);

        if (!transactionsToComplete.Any()) return;

        var transferService = new TransferService();

        foreach (var transaction in transactionsToComplete)
        {
            transaction.Status = TransactionStatus.COMPLETED;

            if (!string.IsNullOrEmpty(transaction.Advert.Seller?.StripeAccountId))
            {
                try
                {
                    var options = new TransferCreateOptions
                    {
                        Amount = (long)(transaction.Advert.Price * 0.9m * 100),
                        Currency = "chf",
                        Destination = transaction.Advert.Seller.StripeAccountId,
                        TransferGroup = $"TRANS_{transaction.TransactionId}"
                    };
                    await transferService.CreateAsync(options, cancellationToken: cancellationToken);
                    _logger.LogInformation($"Auto-released funds for transaction {transaction.TransactionId}");
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, $"Failed to transfer funds for transaction {transaction.TransactionId}");
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
