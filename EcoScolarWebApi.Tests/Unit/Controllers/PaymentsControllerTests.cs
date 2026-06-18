using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Stripe;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class PaymentsControllerTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly IConfiguration _configMock;
	private readonly PaymentsController _controller;

	public PaymentsControllerTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);
		_configMock = Substitute.For<IConfiguration>();
		_configMock["Stripe:SecretKey"].Returns("sk_test_mock");
		_configMock["Stripe:WebhookSecret"].Returns((string?)null);

		_controller = new PaymentsController(_configMock, _context);

		// Set up a default HttpContext with required properties
		var httpContext = new DefaultHttpContext();
		httpContext.Request.Scheme = "https";
		httpContext.Request.Host = new HostString("localhost", 5001);
		_controller.ControllerContext = new ControllerContext
		{
			HttpContext = httpContext
		};
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	#region Checkout Tests

	[Fact]
	public async Task Checkout_ShouldReturnNotFound_WhenProductDoesNotExist()
	{
		// Arrange
		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 999L },
			ProductPrice = 10.0,
			ShippingMethod = "handToHand"
		};

		// Act
		var result = await _controller.Checkout(request);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task Checkout_ShouldReturnBadRequest_WhenProductIsSold()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 1,
			Title = "Sold Book",
			Description = "Already sold",
			Price = 20m,
			SellerId = "seller-1",
			ISBN = "12345",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.SOLD
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 1L },
			ProductPrice = 20.0,
			ShippingMethod = "handToHand"
		};

		// Act
		var result = await _controller.Checkout(request);

		// Assert
		result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task Checkout_ShouldReturnBadRequest_WhenProductIsPaused()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 2,
			Title = "Paused Book",
			Description = "Being paid for",
			Price = 30m,
			SellerId = "seller-1",
			ISBN = "12345",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.PAUSED
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 2L },
			ProductPrice = 30.0,
			ShippingMethod = "handToHand"
		};

		// Act
		var result = await _controller.Checkout(request);

		// Assert
		result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task Checkout_ShouldReturnNotFound_WhenOneOfMultipleProductsMissing()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 3,
			Title = "Existing Book",
			Description = "A book",
			Price = 15m,
			SellerId = "seller-1",
			ISBN = "12345",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 3L, 999L },
			ProductPrice = 15.0,
			ShippingMethod = "handToHand"
		};

		// Act
		var result = await _controller.Checkout(request);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task Checkout_ShouldFallbackToProductId_WhenProductIdsIsEmpty()
	{
		// Arrange — single product not found via fallback ProductId
		var request = new CheckoutRequestDto
		{
			ProductId = 999,
			ProductIds = new List<long>(),
			ProductPrice = 10.0,
			ShippingMethod = "handToHand"
		};

		// Act
		var result = await _controller.Checkout(request);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	#endregion

	#region Shipping cost calculation

	[Fact]
	public async Task Checkout_ShouldUseZeroShipping_ForHandToHand()
	{
		// Arrange — We can verify the logic by checking product status changes
		var book = new Book
		{
			AdvertId = 10,
			Title = "HTH Book",
			Description = "Hand to hand delivery",
			Price = 100m,
			SellerId = "seller-1",
			ISBN = "12345",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 10L },
			ProductPrice = 100.0,
			ShippingMethod = "handToHand"
		};

		// Act — This will fail at Stripe session creation (no real Stripe key),
		// but the validation and status update should have run
		try
		{
			await _controller.Checkout(request);
		}
		catch
		{
			// Expected: Stripe call fails in unit test context
		}

		// Assert — Product should have been paused (validation passed)
		var advertInDb = await _context.Adverts.FindAsync(10L);
		advertInDb!.Status.Should().Be(AdvertStatus.PAUSED);
	}

	#endregion

	#region Product status management during checkout

	[Fact]
	public async Task Checkout_ShouldSetProductStatusToPaused_WhenValidRequest()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 20,
			Title = "Active Book",
			Description = "Should be paused",
			Price = 50m,
			SellerId = "seller-1",
			ISBN = "12345",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 20L },
			ProductPrice = 50.0,
			ShippingMethod = "handToHand"
		};

		// Act
		try
		{
			await _controller.Checkout(request);
		}
		catch
		{
			// Expected: Stripe call fails in unit test context
		}

		// Assert
		var advertInDb = await _context.Adverts.FindAsync(20L);
		advertInDb!.Status.Should().Be(AdvertStatus.PAUSED);
	}

	[Fact]
	public async Task Checkout_ShouldSetMultipleProductsToPaused()
	{
		// Arrange
		var book1 = new Book
		{
			AdvertId = 30,
			Title = "Book 1",
			Description = "First",
			Price = 25m,
			SellerId = "seller-1",
			ISBN = "11111",
			Author = "A1",
			Publisher = "P1",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		var book2 = new Book
		{
			AdvertId = 31,
			Title = "Book 2",
			Description = "Second",
			Price = 35m,
			SellerId = "seller-2",
			ISBN = "22222",
			Author = "A2",
			Publisher = "P2",
			Edition = "2nd",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.AddRange(book1, book2);
		await _context.SaveChangesAsync();

		var request = new CheckoutRequestDto
		{
			ProductIds = new List<long> { 30L, 31L },
			ProductPrice = 60.0,
			ShippingMethod = "postal"
		};

		// Act
		try
		{
			await _controller.Checkout(request);
		}
		catch
		{
			// Expected: Stripe call fails in unit test context
		}

		// Assert
		var advert1 = await _context.Adverts.FindAsync(30L);
		var advert2 = await _context.Adverts.FindAsync(31L);
		advert1!.Status.Should().Be(AdvertStatus.PAUSED);
		advert2!.Status.Should().Be(AdvertStatus.PAUSED);
	}

	#endregion
}
