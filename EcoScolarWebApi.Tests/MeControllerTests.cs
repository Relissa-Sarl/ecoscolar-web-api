using Xunit;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace EcoScolarWebApi.Tests;

public class MeControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly MeController _controller;

    public MeControllerTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        _controller = new MeController(_context);
    }

    private void SetUserContext(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetUnauthorizedContext()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // No Claims
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetMyPurchases_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        SetUnauthorizedContext();

        // Act
        var result = await _controller.GetMyPurchases();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMyPurchases_ShouldReturnPurchases_WhenUserHasTransactions()
    {
        // Arrange
        var buyerId = "buyer-123";
        SetUserContext(buyerId);

        var seller = new User { Id = "seller-456", Nickname = "CoolSeller" };
        var advert = new Book
        {
            AdvertId = 1,
            Title = "Math Book",
            Description = "Great book",
            Price = 15.50m,
            SellerId = seller.Id,
            Seller = seller,
            ISBN = "123",
            Author = "John",
            Publisher = "Pub",
            Edition = "1st",
            WrittenLanguage = LanguageEnum.FR
        };
        var transaction = new Transaction
        {
            TransactionId = 100,
            BuyerId = buyerId,
            AdvertId = advert.AdvertId,
            Advert = advert,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.COMPLETED
        };

        _context.Users.Add(seller);
        _context.Adverts.Add(advert);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyPurchases();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var purchases = okResult.Value.Should().BeAssignableTo<IEnumerable<PurchaseReadDto>>().Subject.ToList();

        purchases.Should().HaveCount(1);
        purchases[0].AdvertTitle.Should().Be("Math Book");
        purchases[0].Price.Should().Be(15.50m);
        purchases[0].SellerName.Should().Be("CoolSeller");
        purchases[0].Status.Should().Be(TransactionStatus.COMPLETED.ToString());
    }

    [Fact]
    public async Task GetMySales_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        SetUnauthorizedContext();

        // Act
        var result = await _controller.GetMySales();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMySales_ShouldReturnSalesWithAndWithoutBuyers()
    {
        // Arrange
        var sellerId = "seller-123";
        SetUserContext(sellerId);

        var seller = new User { Id = sellerId, Nickname = "MySeller" };
        var buyer = new User { Id = "buyer-456", Nickname = "LuckyBuyer" };
        _context.Users.AddRange(seller, buyer);

        // Active advert (not sold)
        var activeAdvert = new Book
        {
            AdvertId = 1,
            Title = "Active Book",
            Description = "On sale",
            Price = 20m,
            SellerId = sellerId,
            ISBN = "123",
            Author = "John",
            Publisher = "Pub",
            Edition = "1st",
            WrittenLanguage = LanguageEnum.FR,
            Status = AdvertStatus.ACTIVE
        };

        // Sold advert
        var soldAdvert = new Book
        {
            AdvertId = 2,
            Title = "Sold Book",
            Description = "Already sold",
            Price = 10m,
            SellerId = sellerId,
            ISBN = "456",
            Author = "Jane",
            Publisher = "Pub",
            Edition = "1st",
            WrittenLanguage = LanguageEnum.FR,
            Status = AdvertStatus.SOLD
        };

        var transaction = new Transaction
        {
            TransactionId = 200,
            BuyerId = buyer.Id,
            Buyer = buyer,
            AdvertId = soldAdvert.AdvertId,
            Advert = soldAdvert,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.COMPLETED
        };

        _context.Adverts.AddRange(activeAdvert, soldAdvert);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMySales();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sales = okResult.Value.Should().BeAssignableTo<IEnumerable<AdvertReadDto>>().Subject.ToList();

        sales.Should().HaveCount(2);

        var activeSaleDto = sales.Single(s => s.Id.ToString() == activeAdvert.AdvertId.ToString());
        activeSaleDto.Title.Should().Be("Active Book");
        activeSaleDto.BuyerName.Should().BeNullOrEmpty("parce que l'article n'est pas encore vendu");

        var soldSaleDto = sales.Single(s => s.Id.ToString() == soldAdvert.AdvertId.ToString());
        soldSaleDto.Title.Should().Be("Sold Book");
        soldSaleDto.BuyerName.Should().Be("LuckyBuyer", "parce qu'une transaction existe avec cet acheteur");
    }

    [Fact]
    public async Task GetMyPurchases_ShouldIncludeReview_WhenReviewExists()
    {
        // Arrange
        var buyerId = "buyer-123";
        SetUserContext(buyerId);

        var seller = new User { Id = "seller-456", Nickname = "CoolSeller" };
        var advert = new Book
        {
            AdvertId = 1,
            Title = "Math Book",
            Description = "Great book",
            Price = 15.50m,
            SellerId = seller.Id,
            Seller = seller,
            ISBN = "123",
            Author = "John",
            Publisher = "Pub",
            Edition = "1st",
            WrittenLanguage = LanguageEnum.FR
        };
        var transaction = new Transaction
        {
            TransactionId = 100,
            BuyerId = buyerId,
            AdvertId = advert.AdvertId,
            Advert = advert,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.COMPLETED
        };
        var review = new Review
        {
            ReviewId = 1,
            Rating = 5,
            Comment = "Super vendeur, envoi rapide.",
            ReviewerId = buyerId,
            ReviewedId = seller.Id,
            TransactionId = transaction.TransactionId,
            ReviewedRole = ReviewedRole.SELLER
        };

        _context.Users.Add(seller);
        _context.Adverts.Add(advert);
        _context.Transactions.Add(transaction);
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyPurchases();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var purchases = okResult.Value.Should().BeAssignableTo<IEnumerable<PurchaseReadDto>>().Subject.ToList();

        purchases.Should().HaveCount(1);
        purchases[0].Review.Should().NotBeNull();
        purchases[0].Review!.Rating.Should().Be(5);
        purchases[0].Review!.Comment.Should().Be("Super vendeur, envoi rapide.");
    }

    [Fact]
    public async Task GetMySales_ShouldIncludeReview_WhenReviewExists()
    {
        // Arrange
        var sellerId = "seller-123";
        SetUserContext(sellerId);

        var seller = new User { Id = sellerId, Nickname = "MySeller" };
        var buyer = new User { Id = "buyer-456", Nickname = "LuckyBuyer" };
        _context.Users.AddRange(seller, buyer);

        var soldAdvert = new Book
        {
            AdvertId = 2,
            Title = "Sold Book",
            Description = "Great book",
            Price = 10m,
            SellerId = sellerId,
            ISBN = "456",
            Author = "Jane",
            Publisher = "Pub",
            Edition = "1st",
            WrittenLanguage = LanguageEnum.FR,
            Status = AdvertStatus.SOLD
        };

        var transaction = new Transaction
        {
            TransactionId = 200,
            BuyerId = buyer.Id,
            Buyer = buyer,
            AdvertId = soldAdvert.AdvertId,
            Advert = soldAdvert,
            Date = DateTime.UtcNow,
            Status = TransactionStatus.COMPLETED
        };

        var review = new Review
        {
            ReviewId = 2,
            Rating = 4,
            Comment = "Super vendeur",
            ReviewerId = buyer.Id,
            ReviewedId = sellerId,
            TransactionId = transaction.TransactionId,
            ReviewedRole = ReviewedRole.SELLER
        };

        _context.Adverts.Add(soldAdvert);
        _context.Transactions.Add(transaction);
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMySales();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sales = okResult.Value.Should().BeAssignableTo<IEnumerable<AdvertReadDto>>().Subject.ToList();

        var soldSaleDto = sales.Single(s => s.Id.ToString() == soldAdvert.AdvertId.ToString());
        soldSaleDto.Review.Should().NotBeNull();
        soldSaleDto.Review!.Rating.Should().Be(4);
        soldSaleDto.Review!.Comment.Should().Be("Super vendeur");
    }
}
