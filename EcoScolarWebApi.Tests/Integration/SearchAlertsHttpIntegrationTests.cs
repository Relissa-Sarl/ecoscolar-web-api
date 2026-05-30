using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcoScolarWebApi.DTOs.Users;
using FluentAssertions;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// T5 · intégration HTTP alertes de recherche (POST / GET / DELETE /users/me/search-alerts).
/// </summary>
public class SearchAlertsHttpIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
	private readonly AuthInMemoryWebApplicationFactory _factory;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public SearchAlertsHttpIntegrationTests(AuthInMemoryWebApplicationFactory factory) => _factory = factory;

	private static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@example.com";

	private HttpClient CreateClient() => _factory.CreateCookieClient();

	[Fact]
	public async Task SearchAlerts_WithoutAuth_ReturnsUnauthorized()
	{
		_factory.EnsureSeeded();
		var client = _factory.CreateClient();

		var response = await client.PostAsJsonAsync("/api/v1/users/me/search-alerts", new { q = "Biologie" });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CreateSearchAlert_WithEmptyBody_ReturnsBadRequest()
	{
		var client = CreateClient();
		var email = UniqueEmail("alert-empty");

		await RegisterAndLoginAsync(client, email);

		var response = await client.PostAsJsonAsync("/api/v1/users/me/search-alerts", new { });

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task SearchAlerts_FullCrudFlow_WithCookieAuth_Succeeds()
	{
		var client = CreateClient();
		var email = UniqueEmail("alert-crud");

		await RegisterAndLoginAsync(client, email);

		var createResponse = await client.PostAsJsonAsync("/api/v1/users/me/search-alerts", new { q = "Biologie" });
		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

		var created = await createResponse.Content.ReadFromJsonAsync<SearchAlertReadDto>(JsonOptions);
		created.Should().NotBeNull();
		created!.Q.Should().Be("Biologie");
		created.Id.Should().BeGreaterThan(0);

		var listResponse = await client.GetAsync("/api/v1/users/me/search-alerts");
		listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var alerts = await listResponse.Content.ReadFromJsonAsync<List<SearchAlertReadDto>>(JsonOptions);
		alerts.Should().HaveCount(1);
		alerts![0].Q.Should().Be("Biologie");

		var deleteResponse = await client.DeleteAsync($"/api/v1/users/me/search-alerts/{created.Id}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var listAfterDelete = await client.GetAsync("/api/v1/users/me/search-alerts");
		listAfterDelete.StatusCode.Should().Be(HttpStatusCode.OK);

		var emptyAlerts = await listAfterDelete.Content.ReadFromJsonAsync<List<SearchAlertReadDto>>(JsonOptions);
		emptyAlerts.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteSearchAlert_WithUnknownId_ReturnsNotFound()
	{
		var client = CreateClient();
		var email = UniqueEmail("alert-delete-404");

		await RegisterAndLoginAsync(client, email);

		var response = await client.DeleteAsync("/api/v1/users/me/search-alerts/99999");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetMySearchAlerts_ReturnsOnlyCurrentUserAlerts()
	{
		var clientA = CreateClient();
		var clientB = CreateClient();
		var emailA = UniqueEmail("alert-user-a");
		var emailB = UniqueEmail("alert-user-b");

		await RegisterAndLoginAsync(clientA, emailA);
		(await clientA.PostAsJsonAsync("/api/v1/users/me/search-alerts", new { q = "Math" }))
			.StatusCode.Should().Be(HttpStatusCode.Created);

		await RegisterAndLoginAsync(clientB, emailB);

		var listB = await clientB.GetAsync("/api/v1/users/me/search-alerts");
		listB.StatusCode.Should().Be(HttpStatusCode.OK);

		var alertsB = await listB.Content.ReadFromJsonAsync<List<SearchAlertReadDto>>(JsonOptions);
		alertsB.Should().BeEmpty();
	}

	private static async Task RegisterAndLoginAsync(HttpClient client, string email, string password = "Password123!")
	{
		var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
		registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });
		loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
