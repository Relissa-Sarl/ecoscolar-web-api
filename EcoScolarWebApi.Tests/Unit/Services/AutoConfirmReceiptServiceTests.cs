using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Services;

public class AutoConfirmReceiptServiceTests : IDisposable
{
	private readonly ServiceProvider _serviceProvider;
	private readonly DbContextOptions<EcoscolarDbContext> _dbOptions;
	private readonly IConfiguration _configuration;

	public AutoConfirmReceiptServiceTests()
	{
		_dbOptions = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		var services = new ServiceCollection();
		services.AddScoped(_ => new EcoscolarDbContext(_dbOptions));
		_serviceProvider = services.BuildServiceProvider();

		_configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BusinessSettings:SellerAutoPayoutDays"] = "7"
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

	private AutoConfirmReceiptService CreateService()
	{
		var logger = Substitute.For<ILogger<AutoConfirmReceiptService>>();
		return new AutoConfirmReceiptService(_serviceProvider, _configuration, logger);
	}

	[Fact]
	public async Task ExecuteAsync_ShouldAutoConfirm_ShippedTransactionsOlderThan7Days()
	{
		// Arrange
		using (var ctx = CreateContext())
		{
			var seller = new User { Id = "seller-1", Nickname = "Seller1" };
			ctx.Users.Add(seller);

			var book = new Book
			{
				AdvertId = 1,
				Title = "Shipped Book",
				Description = "Shipped long ago",
				Price = 50m,
				SellerId = "seller-1",
				Seller = seller,
				ISBN = "123",
				Author = "Author",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR,
				Status = AdvertStatus.ACTIVE
			};
			ctx.Products.Add(book);

			ctx.Transactions.Add(new Transaction
			{
				TransactionId = 1,
				AdvertId = 1,
				BuyerId = "buyer-1",
				Status = TransactionStatus.SHIPPED,
				ShippedDate = DateTime.UtcNow.AddDays(-8)
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
			var txInDb = await ctx.Transactions.FindAsync(1L);
			txInDb!.Status.Should().Be(TransactionStatus.COMPLETED);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ShouldSetAdvertToSold_WhenAutoConfirmed()
	{
		// Arrange
		using (var ctx = CreateContext())
		{
			var seller = new User { Id = "seller-2", Nickname = "Seller2" };
			ctx.Users.Add(seller);

			var book = new Book
			{
				AdvertId = 2,
				Title = "Book to Confirm",
				Description = "Advert status check",
				Price = 30m,
				SellerId = "seller-2",
				Seller = seller,
				ISBN = "456",
				Author = "Author",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR,
				Status = AdvertStatus.ACTIVE
			};
			ctx.Products.Add(book);

			ctx.Transactions.Add(new Transaction
			{
				TransactionId = 2,
				AdvertId = 2,
				BuyerId = "buyer-2",
				Status = TransactionStatus.SHIPPED,
				ShippedDate = DateTime.UtcNow.AddDays(-10)
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
			advertInDb!.Status.Should().Be(AdvertStatus.SOLD);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ShouldNotConfirm_RecentlyShippedTransactions()
	{
		// Arrange
		using (var ctx = CreateContext())
		{
			var seller = new User { Id = "seller-3", Nickname = "Seller3" };
			ctx.Users.Add(seller);

			var book = new Book
			{
				AdvertId = 3,
				Title = "Recently Shipped",
				Description = "Not yet eligible",
				Price = 20m,
				SellerId = "seller-3",
				Seller = seller,
				ISBN = "789",
				Author = "Author",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR,
				Status = AdvertStatus.ACTIVE
			};
			ctx.Products.Add(book);

			ctx.Transactions.Add(new Transaction
			{
				TransactionId = 3,
				AdvertId = 3,
				BuyerId = "buyer-3",
				Status = TransactionStatus.SHIPPED,
				ShippedDate = DateTime.UtcNow.AddDays(-3) // 3 days ago < 7 day threshold
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
			var txInDb = await ctx.Transactions.FindAsync(3L);
			txInDb!.Status.Should().Be(TransactionStatus.SHIPPED);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ShouldNotConfirm_NonShippedTransactions()
	{
		// Arrange
		using (var ctx = CreateContext())
		{
			var seller = new User { Id = "seller-4", Nickname = "Seller4" };
			ctx.Users.Add(seller);

			var book = new Book
			{
				AdvertId = 4,
				Title = "Waiting Book",
				Description = "Not shipped yet",
				Price = 15m,
				SellerId = "seller-4",
				Seller = seller,
				ISBN = "000",
				Author = "Author",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR,
				Status = AdvertStatus.ACTIVE
			};
			ctx.Products.Add(book);

			ctx.Transactions.Add(new Transaction
			{
				TransactionId = 4,
				AdvertId = 4,
				BuyerId = "buyer-4",
				Status = TransactionStatus.PAID_WAITING_SHIPPING,
				ShippedDate = null
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
			var txInDb = await ctx.Transactions.FindAsync(4L);
			txInDb!.Status.Should().Be(TransactionStatus.PAID_WAITING_SHIPPING);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ShouldNotConfirm_AlreadyCompletedTransactions()
	{
		// Arrange
		using (var ctx = CreateContext())
		{
			var seller = new User { Id = "seller-5", Nickname = "Seller5" };
			ctx.Users.Add(seller);

			var book = new Book
			{
				AdvertId = 5,
				Title = "Completed Book",
				Description = "Already done",
				Price = 10m,
				SellerId = "seller-5",
				Seller = seller,
				ISBN = "555",
				Author = "Author",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR,
				Status = AdvertStatus.SOLD
			};
			ctx.Products.Add(book);

			ctx.Transactions.Add(new Transaction
			{
				TransactionId = 5,
				AdvertId = 5,
				BuyerId = "buyer-5",
				Status = TransactionStatus.COMPLETED,
				ShippedDate = DateTime.UtcNow.AddDays(-10)
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
			var txInDb = await ctx.Transactions.FindAsync(5L);
			txInDb!.Status.Should().Be(TransactionStatus.COMPLETED);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ShouldDoNothing_WhenNoTransactionsExist()
	{
		// Arrange — empty database
		var service = CreateService();

		// Act & Assert — should not throw
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(5));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(1000);
			await service.StopAsync(CancellationToken.None);
		}
		catch (OperationCanceledException) { }

		// Assert — no transactions in DB
		using (var ctx = CreateContext())
		{
			var count = await ctx.Transactions.CountAsync();
			count.Should().Be(0);
		}
	}
}
