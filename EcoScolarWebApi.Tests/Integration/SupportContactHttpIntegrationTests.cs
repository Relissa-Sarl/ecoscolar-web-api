using System.Net;
using System.Net.Http.Json;
using EcoScolarWebApi.DTOs.Support;
using FluentAssertions;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class SupportContactHttpIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly AuthInMemoryWebApplicationFactory _factory;

    public SupportContactHttpIntegrationTests(AuthInMemoryWebApplicationFactory factory) => _factory = factory;

    private static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@example.com";

    private HttpClient CreateClient() => _factory.CreateCookieClient();

    [Fact]
    public async Task SubmitSupport_WithoutAuth_ReturnsCreated()
    {
        _factory.EnsureSeeded();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/support", new
        {
            email = "user@example.com",
            subject = "Signaler un bug",
            message = "Le bouton favoris ne répond plus sur mobile."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<SupportContactResponseDto>();
        body.Should().NotBeNull();
        body!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitSupport_ReturnsBadRequest_WhenSubjectTooShort()
    {
        _factory.EnsureSeeded();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/support", new
        {
            email = "user@example.com",
            subject = "Test",
            message = "Message de test suffisamment long."
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitSupport_ReturnsBadRequest_WhenMessageTooShort()
    {
        _factory.EnsureSeeded();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/support", new
        {
            email = "user@example.com",
            subject = "Objet valide",
            message = "court"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMySupportTickets_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/support/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMySupportTickets_AfterSubmit_ReturnsUserTickets()
    {
        var client = CreateClient();
        var email = UniqueEmail("support-list");
        const string password = "Password123!";

        await RegisterAndLoginAsync(client, email, password);

        var createResponse = await client.PostAsJsonAsync("/api/v1/support", new
        {
            email,
            subject = "Suivi de commande",
            message = "Ma commande EDU-123 n'est pas arrivée."
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/v1/support/mine");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tickets = await listResponse.Content.ReadFromJsonAsync<List<SupportTicketReadDto>>();
        tickets.Should().HaveCount(1);
        tickets![0].Subject.Should().Be("Suivi de commande");
        tickets[0].Email.Should().Be(email);
    }

    private static async Task RegisterAndLoginAsync(HttpClient client, string email, string password)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
