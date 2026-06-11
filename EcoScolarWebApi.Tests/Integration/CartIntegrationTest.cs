using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcoScolarWebApi.DTOs.Cart;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class CartIntegrationTest : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly AuthInMemoryWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CartIntegrationTest(AuthInMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<(HttpClient client, string email)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateCookieClient();
        var email = $"cart.user.{Guid.NewGuid():N}@example.com";

        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Password123!" });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password = "Password123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (client, email);
    }

    private async Task<long> SeedAdvertAsync(string sellerEmail, string title, decimal price)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();

        var seller = await db.Users.FirstOrDefaultAsync(u => u.Email == sellerEmail);
        if (seller == null)
        {
            seller = new User
            {
                UserName = sellerEmail,
                Email = sellerEmail,
                Nickname = "SellerNick",
                IsOnboarded = true
            };
            db.Users.Add(seller);
            await db.SaveChangesAsync();
        }

        var item = new PhysicalItem
        {
            Title = title,
            Description = "Description of " + title,
            Price = price,
            SellerId = seller.Id,
            Status = AdvertStatus.ACTIVE,
            NotificationDate = DateTime.UtcNow,
            Condition = PhysicalItemCondition.NEW,
            ProductCategoryId = 1
        };

        db.Products.Add(item);
        await db.SaveChangesAsync();

        return item.AdvertId;
    }

    [Fact]
    public async Task GetCartItems_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCartItems_WhenCartIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<CartItemDto>>(JsonOptions);
        items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task PostCartItem_WithValidAdvert_AddsToCart()
    {
        // Arrange
        var (client, userEmail) = await CreateAuthenticatedClientAsync();
        var advertId = await SeedAdvertAsync("seller@example.com", "Vidéoprojecteur", 150m);

        var dto = new AddToCartDto { AdvertId = advertId };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cart", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdDto = await response.Content.ReadFromJsonAsync<CartItemDto>(JsonOptions);
        createdDto.Should().NotBeNull();
        createdDto!.AdvertId.Should().Be(advertId);
        createdDto.Title.Should().Be("Vidéoprojecteur");
        createdDto.Price.Should().Be(150m);
        createdDto.SellerPseudo.Should().Be("SellerNick");
        createdDto.Type.Should().Be("PRODUCT");

        // Verify it is in the cart
        var getResponse = await client.GetAsync("/api/v1/cart");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await getResponse.Content.ReadFromJsonAsync<List<CartItemDto>>(JsonOptions);
        items.Should().ContainSingle(i => i.AdvertId == advertId);
    }

    [Fact]
    public async Task PostCartItem_WhenAdvertDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var dto = new AddToCartDto { AdvertId = 999999 }; // Non-existent ID

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cart", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostCartItem_WhenAlreadyInCart_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var advertId = await SeedAdvertAsync("seller.dup@example.com", "Classeur rouge", 5m);

        var dto = new AddToCartDto { AdvertId = advertId };

        // Add once
        var response1 = await client.PostAsJsonAsync("/api/v1/cart", dto);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Add again
        var response2 = await client.PostAsJsonAsync("/api/v1/cart", dto);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCartItem_WhenAddingOwnAdvert_ReturnsBadRequest()
    {
        // Arrange
        var (client, userEmail) = await CreateAuthenticatedClientAsync();
        var advertId = await SeedAdvertAsync(userEmail, "Mon propre classeur", 5m);

        var dto = new AddToCartDto { AdvertId = advertId };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cart", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCartItem_WhenItemIsInCart_RemovesIt()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var advertId = await SeedAdvertAsync("seller.del@example.com", "Règle en métal", 3.50m);

        var dto = new AddToCartDto { AdvertId = advertId };
        await client.PostAsJsonAsync("/api/v1/cart", dto);

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/cart/{advertId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it is no longer in the cart
        var getResponse = await client.GetAsync("/api/v1/cart");
        var items = await getResponse.Content.ReadFromJsonAsync<List<CartItemDto>>(JsonOptions);
        items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task DeleteCartItem_WhenItemIsNotInCart_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync("/api/v1/cart/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
