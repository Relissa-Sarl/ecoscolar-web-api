using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.Enums;
using Xunit;

public class CreateBookAdvertTest : IClassFixture<CustomApiFactory>
{
	private readonly HttpClient _client;
	private readonly CustomApiFactory _factory;

	public CreateBookAdvertTest(CustomApiFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();

		// Reset database before test
		factory.ResetDatabaseAsync().GetAwaiter().GetResult();
	}

	[Fact]
	public async Task CreateBookAdvert_WhenAuthenticated_ReturnsCreated()
	{
		string userId;
		using (var scope = _factory.Services.CreateScope())
		{
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
			var email = "test@example.com";
			var existingUser = await userManager.FindByEmailAsync(email);
			if (existingUser == null)
			{
				var user = new User
				{
					UserName = email,
					Email = email,
					FirstName = "Test",
					LastName = "User"
				};
				var result = await userManager.CreateAsync(user, "Password123!");
				if (!result.Succeeded)
				{
					throw new Exception("Failed to create test user");
				}
				userId = user.Id;
			}
			else
			{
				userId = existingUser.Id;
			}
		}

		// Log in the user to obtain the token
		var loginRequest = new
		{
			Email = "test@example.com",
			Password = "Password123!"
		};

		var loginResponse = await _client.PostAsJsonAsync("/login", loginRequest);
		Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

		var identityResponse = await loginResponse.Content.ReadFromJsonAsync<IdentityLoginResponse>();
		Assert.NotNull(identityResponse);
		string token = identityResponse.AccessToken;

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		// Payload for book advert creation conforming to BookCreateDto
		var newAdRequest = new
		{
			Title = "Harry Potter et l'École des Sorciers",
			Description = "Livre en excellent état, édition 2020.",
			Price = 10.50m,
			UserId = userId,
			Images = new string[] { "image.png" },
			Condition = Condition.NEW,
			CategoryId = 1L,
			Isbn = "9782070584628",
			Author = "J.K. Rowling",
			Publisher = "Gallimard",
			Edition = "2020",
			WrittenLanguage = Language.FR
		};

		// Send POST request
		var response = await _client.PostAsJsonAsync("/api/v1/adverts/books", newAdRequest);

		// Verify if advert is created
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	// JSON response mapping
	private class IdentityLoginResponse
	{
		public string TokenType { get; set; } = default!;
		public string AccessToken { get; set; } = default!;
		public int ExpiresIn { get; set; }
		public string RefreshToken { get; set; } = default!;
	}
}
