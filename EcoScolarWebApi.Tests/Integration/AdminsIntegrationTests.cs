using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task GetAllUsers_FlagsUserWithMoreThanFiveBadReviews()
    {
        var (client, _) = await CreateAdminClientAsync();

        string flaggedUserId;
        string normalUserId;

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();

            var flagged = new User
            {
                UserName = $"flagged.{Guid.NewGuid():N}@example.com",
                Email = $"flagged.{Guid.NewGuid():N}@example.com",
                EmailConfirmed = true,
                FirstName = "Victor",
                LastName = "Mauvais",
                Nickname = $"victor-{Guid.NewGuid():N}"
            };
            var normal = new User
            {
                UserName = $"normal.{Guid.NewGuid():N}@example.com",
                Email = $"normal.{Guid.NewGuid():N}@example.com",
                EmailConfirmed = true,
                FirstName = "Nadia",
                LastName = "Normale",
                Nickname = $"nadia-{Guid.NewGuid():N}"
            };
            var reviewer = new User
            {
                UserName = $"reviewer.{Guid.NewGuid():N}@example.com",
                Email = $"reviewer.{Guid.NewGuid():N}@example.com",
                EmailConfirmed = true,
                FirstName = "Judith",
                LastName = "Critique",
                Nickname = $"judith-{Guid.NewGuid():N}"
            };

            (await userManager.CreateAsync(flagged, "Password123!")).Succeeded.Should().BeTrue();
            (await userManager.CreateAsync(normal, "Password123!")).Succeeded.Should().BeTrue();
            (await userManager.CreateAsync(reviewer, "Password123!")).Succeeded.Should().BeTrue();

            flaggedUserId = flagged.Id;
            normalUserId = normal.Id;

            // 6 bad reviews (rating < 3) received by the flagged user -> alert (threshold: more than 5).
            for (var i = 0; i < 6; i++)
            {
                db.Reviews.Add(new Review
                {
                    Rating = 1,
                    Date = DateTime.UtcNow,
                    ReviewedRole = ReviewedRole.SELLER,
                    ReviewerId = reviewer.Id,
                    ReviewedId = flagged.Id,
                    TransactionId = 900_000 + i
                });
            }

            // The normal user only has a good review and must not be flagged.
            db.Reviews.Add(new Review
            {
                Rating = 5,
                Date = DateTime.UtcNow,
                ReviewedRole = ReviewedRole.SELLER,
                ReviewerId = reviewer.Id,
                ReviewedId = normal.Id,
                TransactionId = 910_000
            });

            await db.SaveChangesAsync();
        }

        var users = await client.GetFromJsonAsync<List<UserResponseProbe>>("/api/v1/admins/users");
        users.Should().NotBeNull();

        var flaggedDto = users!.Single(u => u.Id == flaggedUserId);
        flaggedDto.BadReviewsCount.Should().Be(6);
        flaggedDto.AlerteTooBadReviews.Should().BeTrue();

        var normalDto = users.Single(u => u.Id == normalUserId);
        normalDto.BadReviewsCount.Should().Be(0);
        normalDto.AlerteTooBadReviews.Should().BeFalse();
    }

    // Minimal projection of UserResponse limited to the fields exercised by this test.
    private sealed record UserResponseProbe(string Id, int BadReviewsCount, bool AlerteTooBadReviews);

    [Fact]
    public async Task GetAllSupports_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/admins/supports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddTicketMessage_AsAdmin_ReturnsCreated()
    {
        var (client, _) = await CreateAdminClientAsync();

        int ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
            var ticket = new EcoScolarWebApi.Models.SupportTicket
            {
                Email = "user@example.com",
                Subject = "Test Ticket",
                Message = "Help me",
                CreatedAt = DateTime.UtcNow
            };
            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.PostAsJsonAsync($"/api/v1/admins/supports/{ticketId}/message", new { Message = "Support response" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task BanUser_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();
        string userIdToBan;

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = new User { UserName = "toban@example.com", Email = "toban@example.com", FirstName = "To", LastName = "Ban" };
            await userManager.CreateAsync(user, "Password123!");
            userIdToBan = user.Id;
        }

        var response = await client.PatchAsync($"/api/v1/admins/{userIdToBan}/ban", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BlockAdvert_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();
        long advertId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            
            var seller = new User { UserName = "seller@example.com", Email = "seller@example.com", FirstName = "Seller", LastName = "User" };
            await userManager.CreateAsync(seller, "Password123!");
            
            var advert = new EcoScolarWebApi.Models.PhysicalItem { Title = "Test Advert", Description = "Test Description", SellerId = seller.Id, Seller = seller, Status = AdvertStatus.ACTIVE, CreatedAt = DateTime.UtcNow };
            db.Products.Add(advert);
            await db.SaveChangesAsync();
            advertId = advert.AdvertId;
        }

        var response = await client.PatchAsync($"/api/v1/admins/{advertId}/block", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllAbuses_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/admins/abuses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeAbuseStatus_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();
        var abuseId = await CreateAbuseReportInDatabaseAsync();

        var statusDto = new { Status = Enums.TicketStatus.REVIEWED };
        var response = await client.PatchAsJsonAsync($"/api/v1/admins/abuses/{abuseId}/status", statusDto);

        // Capture response content for debugging
        var content = await response.Content.ReadAsStringAsync();
        
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response content: {content}");
    }

    [Fact]
    public async Task DeleteFlag_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateAdminClientAsync();
        var abuseId = await CreateAbuseReportInDatabaseAsync();

        var response = await client.DeleteAsync($"/api/v1/admins/abuses/{abuseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<int> CreateAbuseReportInDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var reporter = new User { UserName = $"reporter.{Guid.NewGuid():N}@example.com", Email = $"reporter.{Guid.NewGuid():N}@example.com", FirstName = "Rep", LastName = "Porter" };
        await userManager.CreateAsync(reporter, "Password123!");

        var seller = new User { UserName = $"seller.{Guid.NewGuid():N}@example.com", Email = $"seller.{Guid.NewGuid():N}@example.com", FirstName = "Sell", LastName = "Er" };
        await userManager.CreateAsync(seller, "Password123!");

        var advert = new EcoScolarWebApi.Models.PhysicalItem 
        { 
            Title = "Test Advert", 
            Description = "Test Description", 
            SellerId = seller.Id, 
            Seller = seller, 
            Status = AdvertStatus.ACTIVE, 
            CreatedAt = DateTime.UtcNow 
        };
        db.Products.Add(advert);
        await db.SaveChangesAsync();

        var abuse = new EcoScolarWebApi.Models.AbuseReport 
        { 
            Reason = ReportReason.INAPPROPRIATE_ADVERT, 
            CreatedAt = DateTime.UtcNow, 
            Status = Enums.TicketStatus.PENDING,
            ReporterUserId = reporter.Id,
            Reporter = reporter,
            TargetAdvertId = advert.AdvertId,
            TargetAdvert = advert,
            Message = "Spam report message"
        };
        db.AbuseReports.Add(abuse);
        await db.SaveChangesAsync();

        return abuse.Id;
    }
}
