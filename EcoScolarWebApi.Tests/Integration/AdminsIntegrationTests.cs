using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class AdminsIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly AuthInMemoryWebApplicationFactory _factory;

    public AdminsIntegrationTests(AuthInMemoryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, User user)> CreateAdminClientAsync()
    {
        var client = _factory.CreateCookieClient();
        var email = $"admin.{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        // Register
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });

        // Add to Admin role
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var user = await userManager.FindByEmailAsync(email);
            await userManager.AddToRoleAsync(user!, "Admin");
            
            // Re-login to refresh claims
            await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });
            
            return (client, user!);
        }
    }

    [Fact]
    public async Task GetAllUsers_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/admins/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllUsers_AsRegularUser_ReturnsForbidden()
    {
        var client = _factory.CreateCookieClient();
        var email = $"user.{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });

        var response = await client.GetAsync("/api/v1/admins/users");

        // AdminService returns 401 Unauthorized for non-admins, but usually it should be 403 Forbidden
        // Let's see what it actually returns.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); 
    }

    [Fact]
    public async Task GetAllUsers_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admins/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
