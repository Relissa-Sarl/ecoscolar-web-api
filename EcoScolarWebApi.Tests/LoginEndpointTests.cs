using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using EcoscolarWebApi.Models;
using Xunit;

// On injecte la Factory via IClassFixture
public class LoginEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public LoginEndpointTests(CustomApiFactory factory)
    {
        _factory = factory;
        // CreateClient() démarre l'API en mémoire et nous donne un client HTTP configuré pour l'attaquer
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // 1. Arrange : Préparer la base de données
        // On récupère un scope d'injection de dépendances pour accéder au UserManager
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // On vérifie que l'utilisateur n'existe pas déjà pour éviter les conflits si d'autres tests tournent
            var existingUser = await userManager.FindByEmailAsync("test@example.com");
            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = "test@example.com",
                    Email = "test@example.com"
                };
                
                // UserManager va se charger de hasher le mot de passe correctement
                var result = await userManager.CreateAsync(user, "Password123!");
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create test user");
                }
            }
        }

        // Le payload que l'on va envoyer à l'API (avec le mot de passe en clair)
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // 2. Act : Appeler le endpoint /login de MapIdentityApi
        var response = await _client.PostAsJsonAsync("/login", loginRequest);

        // 3. Assert : Vérifier le comportement
        // On s'assure que le status code est 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // On désérialise la réponse JSON de l'API Identity
        var loginResponse = await response.Content.ReadFromJsonAsync<IdentityLoginResponse>();

        // On vérifie que le token d'accès est bien présent
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
    }

    // Petite classe interne pour mapper la réponse JSON de MapIdentityApi
    private class IdentityLoginResponse
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
    }
}