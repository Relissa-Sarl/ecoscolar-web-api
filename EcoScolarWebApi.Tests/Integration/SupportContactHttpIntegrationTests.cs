using Xunit;
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

        var response = await client.PostAsJsonAsync("/api/v1/tickets", new
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

        var response = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            email = "user@example.com",
            subject = "te",
            message = "Message de test suffisamment long."
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitSupport_ReturnsBadRequest_WhenMessageTooShort()
    {
        _factory.EnsureSeeded();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/tickets", new
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
        var response = await client.GetAsync("/api/v1/tickets");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMySupportTickets_AfterSubmit_ReturnsUserTickets()
    {
        var client = CreateClient();
        var email = UniqueEmail("support-list");
        const string password = "Password123!";

        await RegisterAndLoginAsync(client, email, password);

        var createResponse = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            email,
            subject = "Suivi de commande",
            message = "Ma commande EDU-123 n'est pas arrivée."
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/v1/tickets");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tickets = await listResponse.Content.ReadFromJsonAsync<List<SupportTicketSummaryDto>>();
        tickets.Should().HaveCount(1);
        tickets![0].Subject.Should().Be("Suivi de commande");
        tickets[0].Email.Should().Be(email);
    }

    [Fact]
    public async Task GetMySupportTicket_WithMessages_AllowsConversation()
    {
        var client = CreateClient();
        var email = UniqueEmail("support-chat");
        const string password = "Password123!";

        await RegisterAndLoginAsync(client, email, password);

        var createResponse = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            email,
            subject = "Question technique",
            message = "Mon application plante au démarrage sur Windows."
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<SupportContactResponseDto>();

        var detailResponse = await client.GetAsync($"/api/v1/tickets/{created!.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var messagesResponse = await client.GetAsync($"/api/v1/tickets/{created.Id}/messages");
        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await messagesResponse.Content.ReadFromJsonAsync<List<SupportTicketMessageDto>>();
        messages.Should().NotBeNull();
        messages!.Should().Contain(m => m.IsFromSupport);

        var replyResponse = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{created.Id}/messages",
            new { message = "J'ai aussi essayé sur un autre navigateur." });
        replyResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var messagesAfter = await client.GetAsync($"/api/v1/tickets/{created.Id}/messages");
        var allMessages = await messagesAfter.Content.ReadFromJsonAsync<List<SupportTicketMessageDto>>();
        allMessages.Should().Contain(m => !m.IsFromSupport && m.Body.Contains("navigateur"));
    }

    private static async Task RegisterAndLoginAsync(HttpClient client, string email, string password)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
