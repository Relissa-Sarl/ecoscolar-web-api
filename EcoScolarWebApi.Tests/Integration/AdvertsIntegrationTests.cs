using Xunit;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class AdvertsIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly AuthInMemoryWebApplicationFactory _factory;

    public AdvertsIntegrationTests(AuthInMemoryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAdverts_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/adverts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBook_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var dto = new BookCreateDto(
            Title: "Unauthorized Book",
            Description: "Should be protected",
            Price: 10m,
            UserId: "any-user",
            Images: [],
            Condition: PhysicalItemCondition.NEW,
            CategoryId: 1,
            Isbn: "1234567890",
            Author: "Author",
            Publisher: "Publisher",
            Edition: "1st",
            WrittenLanguage: LanguageEnum.FR
        );

        var response = await client.PostAsJsonAsync("/api/v1/adverts/books", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBook_AsOtherUser_ReturnsForbidden()
    {
        var client = _factory.CreateCookieClient();
        var email = $"user.{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        // Register and login
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });

        var dto = new BookCreateDto(
            Title: "Fake Book",
            Description: "Trying to create for another user",
            Price: 10m,
            UserId: "some-other-id", // Not the current user
            Images: [],
            Condition: PhysicalItemCondition.NEW,
            CategoryId: 1,
            Isbn: "1234567890",
            Author: "Author",
            Publisher: "Publisher",
            Edition: "1st",
            WrittenLanguage: LanguageEnum.FR
        );

        var response = await client.PostAsJsonAsync("/api/v1/adverts/books", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
