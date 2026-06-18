using Xunit;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NSubstitute;

namespace EcoScolarWebApi.Tests.Integration;

public class TutoringEscrowIntegrationTests : IClassFixture<CustomApiFactory>, IAsyncLifetime
{
    private readonly CustomApiFactory _factory;

    public TutoringEscrowIntegrationTests(CustomApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProcessDueTransactionsAsync_ShouldCancelAndRefund_WhenAcceptanceTimeoutExceeded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();

        var payoutServiceMock = Substitute.For<IPayoutService>();
        var refundServiceMock = Substitute.For<IRefundService>();

        // Let's replace the services in the scope if we can, or just instantiate the processor
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EcoScolarWebApi.Services.TutoringEscrowProcessor>>();
        var processor = new EcoScolarWebApi.Services.TutoringEscrowProcessor(db, payoutServiceMock, refundServiceMock, configuration, logger);

        var student = new User { UserName = $"student.{Guid.NewGuid():N}", Email = "student@example.com" };
        var tutor = new User { UserName = $"tutor.{Guid.NewGuid():N}", Email = "tutor@example.com" };
        db.Users.AddRange(student, tutor);

        var subject = new Subject { Name = "Math", Code = "MTH", NameFr = "Maths", NameDe = "Mathe", NameIt = "Matematica" };
        var grade = new SchoolGrade { Name = "High School", Code = "HS", NameFr = "Lycée", NameDe = "Gymnasium", NameIt = "Liceo" };
        db.Subjects.Add(subject);
        db.SchoolGrades.Add(grade);

        var advert = new TutoringAdvert
        {
            Title = "Math",
            Description = "Good",
            Price = 10,
            Status = AdvertStatus.ACTIVE,
            Subject = subject,
            SchoolGrade = grade,
            TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR,
            StudyLevel = "High School",
            MaxHours = 5,
            Seller = tutor,
            SellerId = tutor.Id,
            CreatedAt = DateTime.UtcNow,
            NotificationDate = DateTime.UtcNow.AddDays(30)
        };
        db.Adverts.Add(advert);

        var transaction = new Transaction
        {
            BuyerId = student.Id,
            Advert = advert,
            AdvertId = advert.AdvertId,
            Status = TransactionStatus.PAID_WAITING_ACCEPTANCE,
            Amount = 100,
            Date = DateTime.UtcNow.AddDays(-16), // Over 15 days ago
            StripeSessionId = "pay_123",
            PlatformFee = 5
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Act
        await processor.ProcessDueTransactionsAsync(CancellationToken.None);

        // Assert
        var updatedTransaction = await db.Transactions.FirstAsync(t => t.TransactionId == transaction.TransactionId);

        updatedTransaction.Status.Should().Be(TransactionStatus.CANCELLED);
        await refundServiceMock.Received(1).RefundAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }
}
