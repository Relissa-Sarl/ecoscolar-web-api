using Xunit;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class AdvertExpirationServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly DbContextOptions<EcoscolarDbContext> _dbOptions;
    private readonly IEmailSenderService _emailSenderMock;
    private readonly IConfiguration _configuration;

    public AdvertExpirationServiceTests()
    {
        _emailSenderMock = Substitute.For<IEmailSenderService>();

        _dbOptions = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddScoped(_ => new EcoscolarDbContext(_dbOptions));
        services.AddScoped<IEmailSenderService>(_ => _emailSenderMock);
        _serviceProvider = services.BuildServiceProvider();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BusinessSettings:AdvertExpirationDays"] = "30",
                ["BusinessSettings:AdvertNotificationDays"] = "7",
                ["Frontend:BaseUrl"] = "http://localhost:3000"
            })
            .Build();
    }

    public void Dispose()
    {
        using var context = new EcoscolarDbContext(_dbOptions);
        context.Database.EnsureDeleted();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private EcoscolarDbContext CreateContext() => new(_dbOptions);

    private AdvertExpirationService CreateService()
    {
        var logger = Substitute.For<ILogger<AdvertExpirationService>>();
        return new AdvertExpirationService(_serviceProvider, _configuration, logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExpireAdverts_OlderThan30Days()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            ctx.Users.Add(new User { Id = "seller-1", Nickname = "Seller1" });
            ctx.Products.Add(new Book
            {
                AdvertId = 1,
                Title = "Old Book",
                Description = "Should expire",
                Price = 10m,
                SellerId = "seller-1",
                ISBN = "123",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow.AddDays(-31)
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        using (var ctx = CreateContext())
        {
            var advertInDb = await ctx.Adverts.FindAsync(1L);
            advertInDb!.Status.Should().Be(AdvertStatus.EXPIRED);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotExpireAdverts_NewerThan30Days()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            ctx.Users.Add(new User { Id = "seller-2", Nickname = "Seller2" });
            ctx.Products.Add(new Book
            {
                AdvertId = 2,
                Title = "Recent Book",
                Description = "Should not expire",
                Price = 10m,
                SellerId = "seller-2",
                ISBN = "456",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        using (var ctx = CreateContext())
        {
            var advertInDb = await ctx.Adverts.FindAsync(2L);
            advertInDb!.Status.Should().Be(AdvertStatus.ACTIVE);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotExpire_AlreadySoldAdverts()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            ctx.Users.Add(new User { Id = "seller-3", Nickname = "Seller3" });
            ctx.Products.Add(new Book
            {
                AdvertId = 3,
                Title = "Sold Book",
                Description = "Already sold",
                Price = 10m,
                SellerId = "seller-3",
                ISBN = "789",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.SOLD,
                CreatedAt = DateTime.UtcNow.AddDays(-31)
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        using (var ctx = CreateContext())
        {
            var advertInDb = await ctx.Adverts.FindAsync(3L);
            advertInDb!.Status.Should().Be(AdvertStatus.SOLD);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendNotificationEmail_ForAdvertsBetween23And30DaysOld()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            var seller = new User { Id = "seller-4", Nickname = "Seller4", Email = "seller4@test.com" };
            ctx.Users.Add(seller);
            ctx.Products.Add(new Book
            {
                AdvertId = 4,
                Title = "Expiring Soon",
                Description = "Should be notified",
                Price = 10m,
                SellerId = "seller-4",
                Seller = seller,
                ISBN = "111",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                NotificationDate = DateTime.UtcNow.AddDays(-25) // not yet notified for this cycle
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        await _emailSenderMock.Received(1).SendAdvertExpirationWarningAsync(
            Arg.Is<User>(u => u.Id == "seller-4"),
            Arg.Is<Advert>(a => a.AdvertId == 4),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSendNotification_WhenAlreadyNotified()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            var seller = new User { Id = "seller-5", Nickname = "Seller5", Email = "seller5@test.com" };
            ctx.Users.Add(seller);
            ctx.Products.Add(new Book
            {
                AdvertId = 5,
                Title = "Already Notified",
                Description = "Already got email",
                Price = 10m,
                SellerId = "seller-5",
                Seller = seller,
                ISBN = "222",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                NotificationDate = DateTime.UtcNow // already notified
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        await _emailSenderMock.DidNotReceive().SendAdvertExpirationWarningAsync(
            Arg.Any<User>(), Arg.Any<Advert>(), Arg.Any<string>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSendNotification_WhenSellerHasNoEmail()
    {
        // Arrange
        using (var ctx = CreateContext())
        {
            var seller = new User { Id = "seller-6", Nickname = "Seller6", Email = null };
            ctx.Users.Add(seller);
            ctx.Products.Add(new Book
            {
                AdvertId = 6,
                Title = "No Email",
                Description = "Seller has no email",
                Price = 10m,
                SellerId = "seller-6",
                Seller = seller,
                ISBN = "333",
                Author = "Author",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = LanguageEnum.FR,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                NotificationDate = DateTime.UtcNow.AddDays(-25)
            });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(1000);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { }

        // Assert
        await _emailSenderMock.DidNotReceive().SendAdvertExpirationWarningAsync(
            Arg.Any<User>(), Arg.Any<Advert>(), Arg.Any<string>()
        );
    }
}
