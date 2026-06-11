using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.DTOs.Transactions;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace EcoScolarWebApi.Tests;

public class TransactionsControllerTests : IDisposable
{
	private readonly UserManager<User> _userManagerMock;
	private readonly EcoscolarDbContext _context;
	private readonly ReviewMapper _reviewMapper;
	private readonly TransactionsController _controller;

	public TransactionsControllerTests()
	{
		var store = Substitute.For<IUserStore<User>>();
		_userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);

		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);
		_reviewMapper = new ReviewMapper();

		_controller = new TransactionsController(_context, _userManagerMock, _reviewMapper);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	[Fact]
	public async Task CreateReview_ShouldReturnNotFound_WhenTransactionDoesNotExist()
	{
		// Arrange
		var reviewDto = new ReviewRequestDTO(5, "Excellent");

		// Act
		var result = await _controller.CreateReview(999, reviewDto);

		// Assert
		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task CreateReview_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
	{
		// Arrange
		var transactionId = 1L;
		var transaction = new Transaction
		{
			TransactionId = transactionId,
			BuyerId = "buyer-1",
			AdvertId = 10,
			Advert = new Book
			{
				AdvertId = 10,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = "seller-1",
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR
			}
		};
		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		var reviewDto = new ReviewRequestDTO(5, "Excellent");

		// Act
		var result = await _controller.CreateReview(transactionId, reviewDto);

		// Assert
		result.Result.Should().BeOfType<UnauthorizedResult>();
	}

	[Fact]
	public async Task CreateReview_ShouldReturnForbid_WhenUserIsNeitherBuyerNorSeller()
	{
		// Arrange
		var transactionId = 2L;
		var transaction = new Transaction
		{
			TransactionId = transactionId,
			BuyerId = "buyer-1",
			AdvertId = 11,
			Advert = new Book
			{
				AdvertId = 11,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = "seller-1",
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR
			}
		};
		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		var currentUser = new User { Id = "third-party", Nickname = "intruder" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);

		var reviewDto = new ReviewRequestDTO(5, "Excellent");

		// Act
		var result = await _controller.CreateReview(transactionId, reviewDto);

		// Assert
		result.Result.Should().BeOfType<ForbidResult>();
	}

	[Fact]
	public async Task CreateReview_ShouldReturnConflict_WhenReviewAlreadyExistsFromUser()
	{
		// Arrange
		var transactionId = 3L;
		var transaction = new Transaction
		{
			TransactionId = transactionId,
			BuyerId = "buyer-1",
			AdvertId = 12,
			Advert = new Book
			{
				AdvertId = 12,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = "seller-1",
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR
			}
		};
		var existingReview = new Review
		{
			ReviewId = 100,
			TransactionId = transactionId,
			ReviewerId = "buyer-1",
			ReviewedId = "seller-1",
			Comment = "Nice",
			Rating = 4,
			ReviewedRole = ReviewedRole.SELLER
		};
		_context.Transactions.Add(transaction);
		_context.Reviews.Add(existingReview);
		await _context.SaveChangesAsync();

		var currentUser = new User { Id = "buyer-1", Nickname = "buyer" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);

		var reviewDto = new ReviewRequestDTO(5, "Another review");

		// Act
		var result = await _controller.CreateReview(transactionId, reviewDto);

		// Assert
		result.Result.Should().BeOfType<ConflictObjectResult>();
	}

	[Fact]
	public async Task CreateReview_ShouldReturnCreatedAtAction_AndCreateReview_WhenUserIsBuyer()
	{
		// Arrange
		var transactionId = 4L;
		var buyer = new User { Id = "buyer-1", Nickname = "buyer_nick" };
		var seller = new User { Id = "seller-1", Nickname = "seller_nick" };
		var transaction = new Transaction
		{
			TransactionId = transactionId,
			BuyerId = "buyer-1",
			Buyer = buyer,
			AdvertId = 13,
			Advert = new Book
			{
				AdvertId = 13,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = "seller-1",
				Seller = seller,
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR
			}
		};

		_context.Users.AddRange(buyer, seller);
		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(buyer);

		var reviewDto = new ReviewRequestDTO(5, "Great seller, fast shipment!");

		// Act
		var result = await _controller.CreateReview(transactionId, reviewDto);

		// Assert
		var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
		createdResult.ActionName.Should().Be(nameof(_controller.CreateReview));
		createdResult.RouteValues.Should().ContainKey("transactionId");
		createdResult.RouteValues["transactionId"].Should().Be(transactionId);

		var returnedReviews = createdResult.Value.Should().BeAssignableTo<IEnumerable<ReviewResponseDTO>>().Subject.ToList();
		returnedReviews.Should().HaveCount(1);
		returnedReviews[0].Rating.Should().Be(5);
		returnedReviews[0].Comment.Should().Be("Great seller, fast shipment!");
		returnedReviews[0].ReviewerId.Should().Be("buyer-1");
		returnedReviews[0].ReviewedId.Should().Be("seller-1");
		returnedReviews[0].ReviewedRole.Should().Be(ReviewedRole.SELLER);

		// Verify database
		var reviewInDb = await _context.Reviews.FirstOrDefaultAsync(r => r.TransactionId == transactionId && r.ReviewerId == "buyer-1");
		reviewInDb.Should().NotBeNull();
		reviewInDb!.Rating.Should().Be(5);
		reviewInDb!.Comment.Should().Be("Great seller, fast shipment!");
		reviewInDb!.ReviewedId.Should().Be("seller-1");
		reviewInDb!.ReviewedRole.Should().Be(ReviewedRole.SELLER);
	}

	[Fact]
	public async Task CreateReview_ShouldReturnCreatedAtAction_AndCreateReview_WhenUserIsSeller()
	{
		// Arrange
		var transactionId = 5L;
		var buyer = new User { Id = "buyer-2", Nickname = "buyer_nick" };
		var seller = new User { Id = "seller-2", Nickname = "seller_nick" };
		var transaction = new Transaction
		{
			TransactionId = transactionId,
			BuyerId = "buyer-2",
			Buyer = buyer,
			AdvertId = 14,
			Advert = new Book
			{
				AdvertId = 14,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = "seller-2",
				Seller = seller,
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = LanguageEnum.FR
			}
		};

		_context.Users.AddRange(buyer, seller);
		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(seller);

		var reviewDto = new ReviewRequestDTO(4, "Polite buyer!");

		// Act
		var result = await _controller.CreateReview(transactionId, reviewDto);

		// Assert
		var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
		createdResult.ActionName.Should().Be(nameof(_controller.CreateReview));
		createdResult.RouteValues.Should().ContainKey("transactionId");
		createdResult.RouteValues["transactionId"].Should().Be(transactionId);

		var returnedReviews = createdResult.Value.Should().BeAssignableTo<IEnumerable<ReviewResponseDTO>>().Subject.ToList();
		returnedReviews.Should().HaveCount(1);
		returnedReviews[0].Rating.Should().Be(4);
		returnedReviews[0].Comment.Should().Be("Polite buyer!");
		returnedReviews[0].ReviewerId.Should().Be("seller-2");
		returnedReviews[0].ReviewedId.Should().Be("buyer-2");
		returnedReviews[0].ReviewedRole.Should().Be(ReviewedRole.BUYER);

		// Verify database
		var reviewInDb = await _context.Reviews.FirstOrDefaultAsync(r => r.TransactionId == transactionId && r.ReviewerId == "seller-2");
		reviewInDb.Should().NotBeNull();
		reviewInDb!.Rating.Should().Be(4);
		reviewInDb!.Comment.Should().Be("Polite buyer!");
		reviewInDb!.ReviewedId.Should().Be("buyer-2");
		reviewInDb!.ReviewedRole.Should().Be(ReviewedRole.BUYER);
	}

	[Fact]
	public async Task CreateTransactions_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
	{
		// Arrange
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);
		var request = new CreateTransactionRequestDto { AdvertIds = new List<long> { 1L } };

		// Act
		var result = await _controller.CreateTransactions(request);

		// Assert
		result.Should().BeOfType<UnauthorizedResult>();
	}

	[Fact]
	public async Task CreateTransactions_ShouldReturnBadRequest_WhenAdvertIdsEmpty()
	{
		// Arrange
		var currentUser = new User { Id = "buyer-1" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);
		var request = new CreateTransactionRequestDto { AdvertIds = new List<long>() };

		// Act
		var result = await _controller.CreateTransactions(request);

		// Assert
		result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task CreateTransactions_ShouldReturnNotFound_WhenAdvertDoesNotExist()
	{
		// Arrange
		var currentUser = new User { Id = "buyer-1" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);
		var request = new CreateTransactionRequestDto { AdvertIds = new List<long> { 999L } };

		// Act
		var result = await _controller.CreateTransactions(request);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task CreateTransactions_ShouldCreateTransactionsAndSetStatusToSold_WhenValid()
	{
		// Arrange
		var currentUser = new User { Id = "buyer-1" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);

		var book = new Book
		{
			AdvertId = 20,
			Title = "Test Book",
			Description = "Test Book Desc",
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

		var request = new CreateTransactionRequestDto 
		{ 
			AdvertIds = new List<long> { 20L },
			StripeSessionId = "session-123"
		};

		// Act
		var result = await _controller.CreateTransactions(request);

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		var transactions = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
		transactions.Should().HaveCount(1);

		// Verify database
		var transactionInDb = await _context.Transactions.FirstOrDefaultAsync(t => t.AdvertId == 20L);
		transactionInDb.Should().NotBeNull();
		transactionInDb!.BuyerId.Should().Be("buyer-1");
		transactionInDb.Status.Should().Be(TransactionStatus.PAID_WAITING_SHIPPING);
		transactionInDb.PlatformFee.Should().Be(2.50m); // 5% of 50
		transactionInDb.StripeSessionId.Should().Be("session-123");

		var advertInDb = await _context.Adverts.FindAsync(20L);
		advertInDb.Should().NotBeNull();
		advertInDb!.Status.Should().Be(AdvertStatus.SOLD);
	}

	[Fact]
	public async Task CreateTransactions_ShouldBeIdempotent_WhenCalledMultipleTimes()
	{
		// Arrange
		var currentUser = new User { Id = "buyer-1" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(currentUser);

		var book = new Book
		{
			AdvertId = 30,
			Title = "Test Book 2",
			Description = "Test Book Desc 2",
			Price = 100m,
			SellerId = "seller-1",
			ISBN = "123456",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var request = new CreateTransactionRequestDto 
		{ 
			AdvertIds = new List<long> { 30L },
			StripeSessionId = "session-456"
		};

		// Act (First Call)
		var result1 = await _controller.CreateTransactions(request);

		// Act (Second Call)
		var result2 = await _controller.CreateTransactions(request);

		// Assert
		result1.Should().BeOfType<OkObjectResult>();
		result2.Should().BeOfType<OkObjectResult>();

		var transactionsInDb = await _context.Transactions.Where(t => t.AdvertId == 30L).ToListAsync();
		transactionsInDb.Should().HaveCount(1); // Only 1 transaction created
	}
}
