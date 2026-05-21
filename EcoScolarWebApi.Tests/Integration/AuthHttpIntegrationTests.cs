using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EcoScolarWebApi.DTOs.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// Tests HTTP auth complets : register, login (cookie HttpOnly + bearer), profil, update, logout.
/// </summary>
public class AuthHttpIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
	private readonly AuthInMemoryWebApplicationFactory _factory;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public AuthHttpIntegrationTests(AuthInMemoryWebApplicationFactory factory) => _factory = factory;

	private static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@example.com";

	private HttpClient CreateClient() => _factory.CreateCookieClient();

	private static async Task RegisterAsync(HttpClient client, string email, string password = "Password123!")
	{
		var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Register_WithValidCredentials_CreatesUser()
	{
		var client = CreateClient();
		var email = UniqueEmail("register");

		await RegisterAsync(client, email);

		await LoginWithCookiesAsync(client, email);
		var profile = await GetProfileAsync(client);
		profile.Email.Should().Be(email);
	}

	[Fact]
	public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
	{
		var client = CreateClient();
		var email = UniqueEmail("duplicate");

		await RegisterAsync(client, email);

		var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Password123!" });
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Register_WithWeakPassword_ReturnsBadRequest()
	{
		var client = CreateClient();
		var email = UniqueEmail("weak");

		var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "123" });
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Login_WithValidCredentials_SetsHttpOnlyAuthCookie()
	{
		var client = CreateClient();
		var email = UniqueEmail("cookie");

		await RegisterAsync(client, email);

		using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login?useCookies=true")
		{
			Content = JsonContent.Create(new { email, password = "Password123!" })
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
		cookies!.Should().Contain(c => c.Contains("Ecoscolar.Auth.Session", StringComparison.OrdinalIgnoreCase));
		cookies.Should().Contain(c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
	{
		var client = CreateClient();
		var email = UniqueEmail("badpass");

		await RegisterAsync(client, email);

		var response = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password = "WrongPassword!" });
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
	{
		var client = CreateClient();

		var response = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new
		{
			email = UniqueEmail("unknown"),
			password = "Password123!"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetMyProfile_WithoutAuth_ReturnsUnauthorized()
	{
		_factory.EnsureSeeded();
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/v1/users/me");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetMyProfile_WithCookieAuth_ReturnsUserEmail()
	{
		var client = CreateClient();
		var email = UniqueEmail("profile");

		await RegisterAsync(client, email);
		await LoginWithCookiesAsync(client, email);

		var profile = await GetProfileAsync(client);

		profile.Email.Should().Be(email);
		profile.IsOnboarded.Should().BeFalse();
	}

	[Fact]
	public async Task Login_WithBearerToken_AllowsAccessToProfile()
	{
		var client = CreateClient();
		var email = UniqueEmail("bearer");

		await RegisterAsync(client, email);

		var tokenResponse = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=false", new { email, password = "Password123!" });
		tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var tokens = await tokenResponse.Content.ReadFromJsonAsync<BearerTokenResponse>(JsonOptions);
		tokens.Should().NotBeNull();
		tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
		tokens.TokenType.Should().Be("Bearer");

		var bearerClient = _factory.CreateClient();
		_factory.EnsureSeeded();
		bearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

		var profileResponse = await bearerClient.GetAsync("/api/v1/users/me");
		profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var profile = await profileResponse.Content.ReadFromJsonAsync<UserReadDto>(JsonOptions);
		profile!.Email.Should().Be(email);
	}

	[Fact]
	public async Task UpdateProfile_WithCookieAuth_UpdatesUserAndSetsOnboarded()
	{
		var client = CreateClient();
		var email = UniqueEmail("update");

		await RegisterAsync(client, email);
		await LoginWithCookiesAsync(client, email);

		var updateDto = new UserUpdateDto(
			Nickname: "eco_user",
			FirstName: "Eco",
			LastName: "Scolar",
			PostalCode: "1000",
			BirthdayDate: "2000-01-15",
			SpokenLanguages: [new SpokenLanguageDto("FR", "Native")]
		);

		var response = await client.PutAsJsonAsync("/api/v1/users/me", updateDto);
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var updated = await response.Content.ReadFromJsonAsync<UserReadDto>(JsonOptions);
		updated!.Nickname.Should().Be("eco_user");
		updated.FirstName.Should().Be("Eco");
		updated.LastName.Should().Be("Scolar");
		updated.IsOnboarded.Should().BeTrue();
		updated.Location!.PostalCode.Should().Be("1000");
		updated.Location.City.Should().Be("Lausanne");
		updated.SpokenLanguages.Should().ContainSingle(l => l.Language == "FR" && l.Level == "Native");
	}

	[Fact]
	public async Task UpdateProfile_WithInvalidPostalCode_ReturnsBadRequest()
	{
		var client = CreateClient();
		var email = UniqueEmail("badpostal");

		await RegisterAsync(client, email);
		await LoginWithCookiesAsync(client, email);

		var updateDto = new UserUpdateDto(
			Nickname: "user",
			FirstName: "A",
			LastName: "B",
			PostalCode: "9999",
			BirthdayDate: "2000-01-01",
			SpokenLanguages: [new SpokenLanguageDto("FR", "Native")]
		);

		var response = await client.PutAsJsonAsync("/api/v1/users/me", updateDto);
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Logout_WithCookieAuth_InvalidatesSession()
	{
		var client = CreateClient();
		var email = UniqueEmail("logout");

		await RegisterAsync(client, email);
		await LoginWithCookiesAsync(client, email);

		(await client.GetAsync("/api/v1/users/me")).StatusCode.Should().Be(HttpStatusCode.OK);

		var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
		logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		(await client.GetAsync("/api/v1/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetPublicProfile_AfterOnboarding_ReturnsNickname()
	{
		var client = CreateClient();
		var email = UniqueEmail("public");

		await RegisterAsync(client, email);
		await LoginWithCookiesAsync(client, email);

		var updateDto = new UserUpdateDto(
			Nickname: "public_nick",
			FirstName: "Pub",
			LastName: "Lic",
			PostalCode: "1000",
			BirthdayDate: "1999-05-05",
			SpokenLanguages: [new SpokenLanguageDto("DE", "Fluent")]
		);
		await client.PutAsJsonAsync("/api/v1/users/me", updateDto);

		var profile = await GetProfileAsync(client);

		var publicResponse = await client.GetAsync($"/api/v1/users/{profile.Id}");
		publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var publicProfile = await publicResponse.Content.ReadFromJsonAsync<UserPublicReadDto>(JsonOptions);
		publicProfile!.Nickname.Should().Be("public_nick");
	}

	private static async Task LoginWithCookiesAsync(HttpClient client, string email, string password = "Password123!")
	{
		var response = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private static async Task<UserReadDto> GetProfileAsync(HttpClient client)
	{
		var response = await client.GetAsync("/api/v1/users/me");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		return (await response.Content.ReadFromJsonAsync<UserReadDto>(JsonOptions))!;
	}

	private sealed record BearerTokenResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken);
}
