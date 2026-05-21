using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using EcoscolarWebApi.Models;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class LoginEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public LoginEndpointTests(CustomApiFactory factory)
    {
        _factory = factory;
        // CreateClient() starts API in memory
        _client = factory.CreateClient();

        // Reset database before test
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Injection scope for access to UserManager
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // check that the user does not already exist to avoid conflicts if other tests are running
            var existingUser = await userManager.FindByEmailAsync("test@example.com");
            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = "test@example.com",
                    Email = "test@example.com"
                };
                
                // Hash password 
                var result = await userManager.CreateAsync(user, "Password123!");
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create test user");
                }
            }
        }

        // Payload send to API
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Call endpoint
        var response = await _client.PostAsJsonAsync("/login", loginRequest);

        // Check if the response code is 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Read JSON
        var loginResponse = await response.Content.ReadFromJsonAsync<IdentityLoginResponse>();

        // Check if token is present
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
    }

    // JSON response mapping
    private class IdentityLoginResponse
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
    }
}