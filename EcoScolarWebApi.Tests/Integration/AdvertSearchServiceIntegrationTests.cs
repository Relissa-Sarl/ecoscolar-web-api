using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LanguageEnum = EcoScolarWebApi.Enums.LanguageEnum;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// Intégration service + EF : recherche catalogue livres (UC-06 / T8-4).
/// </summary>
public class AdvertSearchServiceIntegrationTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly AdvertSearchService _service;

	public AdvertSearchServiceIntegrationTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		_context = new EcoscolarDbContext(options);
		_service = new AdvertSearchService(_context);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
	}

	[Fact]
	public async Task SearchSummaries_ByKeyword_ReturnsMatchingBooks()
	{
		await SeedCatalogAsync();

		var results = (await _service.SearchSummariesAsync(new AdvertSearchQuery { Q = "Biologie" })).ToList();

		results.Should().HaveCount(1);
		results[0].Title.Should().Be("Manuel de Biologie");
		results[0].Type.Should().Be(CatalogAdvertTypeCodes.Books);
	}

	[Fact]
	public async Task SearchSummaries_ByIsbn_ReturnsMatchingBook()
	{
		await SeedCatalogAsync();

		var results = (await _service.SearchSummariesAsync(new AdvertSearchQuery { Isbn = "978-3-16-148410-0" })).ToList();

		results.Should().HaveCount(1);
		results[0].Title.Should().Be("Manuel de Biologie");
		results[0].Isbn.Should().Be("978-3-16-148410-0");
	}

	[Fact]
	public async Task SearchSummaries_ByKeyword_DoesNotReturnUnrelatedProducts()
	{
		await SeedCatalogAsync();

		var results = (await _service.SearchSummariesAsync(new AdvertSearchQuery { Q = "Calculatrice" })).ToList();

		results.Should().HaveCount(1);
		results[0].Type.Should().Be(CatalogAdvertTypeCodes.Product);
		results.All(r => r.Type != CatalogAdvertTypeCodes.Books || r.Title.Contains("Calculatrice")).Should().BeTrue();
	}

	[Fact]
	public async Task SearchSummaries_WithoutFilter_ReturnsAllAdverts()
	{
		await SeedCatalogAsync();

		var results = (await _service.SearchSummariesAsync(null)).ToList();

		results.Should().HaveCount(3);
	}

	private async Task SeedCatalogAsync()
	{
		var user = new User
		{
			Id = "user-test-1",
			UserName = "tester",
			FirstName = "Test",
			LastName = "User",
			Email = "test@ecoscolar.local"
		};
		_context.Users.Add(user);

		_context.Adverts.AddRange(
			new Book
			{
				AdvertId = 1,
				Title = "Manuel de Biologie",
				Description = "Livre scolaire",
				Price = 25,
				UserId = user.Id,
				User = user,
				Status = AdvertStatus.ACTIVE,
				CreatedAt = DateTime.UtcNow,
				NotificationDate = DateTime.UtcNow,
				Condition = PhysicalItemCondition.NEW,
				ISBN = "978-3-16-148410-0",
				Author = "Auteur",
				Publisher = "Editeur",
				Edition = "1",
				WrittenLanguage = LanguageEnum.FR,
				BookCategoryId = 1
			},
			new PhysicalItem
			{
				AdvertId = 2,
				Title = "Calculatrice scientifique",
				Description = "Fourniture",
				Price = 40,
				UserId = user.Id,
				User = user,
				Status = AdvertStatus.ACTIVE,
				CreatedAt = DateTime.UtcNow,
				NotificationDate = DateTime.UtcNow,
				Condition = PhysicalItemCondition.LIKE_NEW,
				ProductCategoryId = 9
			},
			new TutoringAdvert
			{
				AdvertId = 3,
				Title = "Cours de mathématiques",
				Description = "Service",
				Price = 30,
				UserId = user.Id,
				User = user,
				Status = AdvertStatus.ACTIVE,
				CreatedAt = DateTime.UtcNow,
				NotificationDate = DateTime.UtcNow,
				StudyLevel = "Collège",
				SubjectId = 4,
				SchoolGradeId = 2
			});

		await _context.SaveChangesAsync();
	}
}
