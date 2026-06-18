using Xunit;
using System.Net;
using System.Net.Http.Json;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Tests.Integration;

public class TutoringTransactionsHttpIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly AuthInMemoryWebApplicationFactory _factory;

    public TutoringTransactionsHttpIntegrationTests(AuthInMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    private async Task<(HttpClient client, User user)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateCookieClient();
        var email = $"user.{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        return (client, user);
    }

    [Fact]
    public async Task AcceptTransaction_ReturnsOk_WhenUserIsSeller()
    {
        // Arrange
        var (client, seller) = await CreateAuthenticatedClientAsync();

        long transactionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
            // Attaching the seller to the db context so we can use it
            db.Attach(seller);

            var buyer = new User { UserName = $"buyer.{Guid.NewGuid():N}@example.com", Email = $"buyer.{Guid.NewGuid():N}@example.com" };
            db.Users.Add(buyer);

            var subject = await db.Subjects.FirstOrDefaultAsync() ?? new Subject { Name = "Math" };
            var grade = await db.SchoolGrades.FirstOrDefaultAsync() ?? new SchoolGrade { Name = "High School", Code = "HS" };
            if (subject.SubjectId == 0) db.Subjects.Add(subject);
            if (grade.SchoolGradeId == 0) db.SchoolGrades.Add(grade);

            var advert = new TutoringAdvert
            {
                Title = "Math lessons",
                Description = "Good",
                Price = 10,
                Status = AdvertStatus.ACTIVE,
                Subject = subject,
                SchoolGrade = grade,
                TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR,
                StudyLevel = "High School",
                MaxHours = 5,
                Seller = seller,
                SellerId = seller.Id,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow.AddDays(30)
            };
            db.Adverts.Add(advert);

            var transaction = new Transaction
            {
                BuyerId = buyer.Id,
                Advert = advert,
                AdvertId = advert.AdvertId,
                Status = TransactionStatus.PAID_WAITING_ACCEPTANCE,
                Amount = 100,
                Date = DateTime.UtcNow,
                StripeSessionId = "pay_123",
                PlatformFee = 5
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            transactionId = transaction.TransactionId;
        }

        // Act
        var response = await client.PatchAsync($"/api/v1/tutoring/transactions/{transactionId}/accept", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
