using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Services;

public class AdvertExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdvertExpirationService> _logger;

    public AdvertExpirationService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AdvertExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdvertExpirationService running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAdvertExpirationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing AdvertExpirationService.");
            }

            // Attendre 24 heures (ou une autre durée configurée) avant de relancer
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessAdvertExpirationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();

        int expirationDays = _configuration.GetValue<int>("BusinessSettings:AdvertExpirationDays", 30);
        int notificationDays = _configuration.GetValue<int>("BusinessSettings:AdvertNotificationDays", 7);
        int notifyThresholdDays = expirationDays - notificationDays; // typically 23

        var thresholdExpire = DateTime.UtcNow.AddDays(-expirationDays);
        var thresholdNotify = DateTime.UtcNow.AddDays(-notifyThresholdDays);

        // 1. Process expirations (>= 30 days)
        var advertsToExpire = await context.Adverts
            .Where(a => a.Status == AdvertStatus.ACTIVE && a.CreatedAt <= thresholdExpire)
            .ToListAsync(stoppingToken);

        if (advertsToExpire.Count > 0)
        {
            _logger.LogInformation($"Found {advertsToExpire.Count} adverts to expire.");
            foreach (var advert in advertsToExpire)
            {
                advert.Status = AdvertStatus.EXPIRED;
            }
        }

        // 2. Process notifications (>= 23 days and < 30 days)
        // We only notify if NotificationDate hasn't been updated for this cycle
        // Using a safe margin, if NotificationDate < CreatedAt + 23 days, it means we haven't sent the warning
        var advertsToNotify = await context.Adverts
            .Include(a => a.Seller)
            .Where(a => a.Status == AdvertStatus.ACTIVE 
                     && a.CreatedAt <= thresholdNotify 
                     && a.CreatedAt > thresholdExpire
                     && a.NotificationDate < a.CreatedAt.AddDays(notifyThresholdDays))
            .ToListAsync(stoppingToken);

        if (advertsToNotify.Count > 0)
        {
            _logger.LogInformation($"Found {advertsToNotify.Count} adverts to notify for expiration warning.");
            
            var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";

            foreach (var advert in advertsToNotify)
            {
                if (advert.Seller?.Email != null)
                {
                    var renewLink = $"{baseUrl.TrimEnd('/')}/me/sales?renew={advert.AdvertId}";
                    await emailSender.SendAdvertExpirationWarningAsync(advert.Seller, advert, renewLink);
                }
                
                advert.NotificationDate = DateTime.UtcNow;
            }
        }

        if (advertsToExpire.Count > 0 || advertsToNotify.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }

        _logger.LogInformation("Advert expiration process completed successfully.");
    }
}
