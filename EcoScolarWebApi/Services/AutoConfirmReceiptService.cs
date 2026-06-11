using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EcoScolarWebApi.Services;

public class AutoConfirmReceiptService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutoConfirmReceiptService> _logger;

    public AutoConfirmReceiptService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AutoConfirmReceiptService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoConfirmReceiptService running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAutoConfirmationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing AutoConfirmReceiptService.");
            }

            // Attendre 24 heures (ou une autre durée configurée) avant de relancer
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessAutoConfirmationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();

        // Récupérer le délai de configuration (par défaut 7)
        int delayDays = _configuration.GetValue<int>("BusinessSettings:SellerAutoPayoutDays", 7);
        var thresholdDate = DateTime.UtcNow.AddDays(-delayDays);

        var transactionsToConfirm = await context.Transactions
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .Where(t => t.Status == TransactionStatus.SHIPPED 
                     && t.ShippedDate.HasValue 
                     && t.ShippedDate.Value <= thresholdDate)
            .ToListAsync(stoppingToken);

        if (transactionsToConfirm.Count == 0)
            return;

        _logger.LogInformation($"Found {transactionsToConfirm.Count} transactions to auto-confirm.");

        var transferService = new TransferService();

        foreach (var transaction in transactionsToConfirm)
        {
            transaction.Status = TransactionStatus.COMPLETED;
            
            if (transaction.Advert != null)
            {
                transaction.Advert.Status = AdvertStatus.SOLD;

                // Trigger Stripe transfer if the seller has a connected account
                if (!string.IsNullOrEmpty(transaction.Advert.Seller?.StripeAccountId))
                {
                    try
                    {
                        var transferOptions = new TransferCreateOptions
                        {
                            Amount = (long)(transaction.Advert.Price * 100), // en centimes
                            Currency = "chf",
                            Destination = transaction.Advert.Seller.StripeAccountId,
                            TransferGroup = "COMMANDE_ID_789", // Same as defined in checkout
                        };

                        await transferService.CreateAsync(transferOptions, cancellationToken: stoppingToken);
                        _logger.LogInformation($"Successfully transferred {transaction.Advert.Price} CHF to seller {transaction.Advert.SellerId} for transaction {transaction.TransactionId}");
                    }
                    catch (StripeException ex)
                    {
                        _logger.LogError(ex, $"Stripe transfer failed for transaction {transaction.TransactionId}");
                    }
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Auto-confirmation process completed successfully.");
    }
}
