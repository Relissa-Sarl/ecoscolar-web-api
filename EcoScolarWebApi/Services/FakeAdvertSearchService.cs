using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Services.Contracts;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Mock catalogue search aligned with <see cref="AdvertSearchService"/>:
/// <c>isbn</c> keeps Books rows whose normalized ISBN matches;
/// <c>q</c> keeps rows whose title contains the keyword (any Adverts type)
/// or whose Books ISBN matches the normalized keyword.
/// </summary>
public class FakeAdvertSearchService : IAdvertSearchService
{
	private sealed record CatalogMockEntry(AdvertSummaryDto Summary, string Description);

	private static readonly IReadOnlyList<CatalogMockEntry> Catalog = BuildCatalog();

	private static readonly IReadOnlyList<AdvertSummaryDto> Summaries =
		Catalog.Select(e => e.Summary).ToList();

	public Task<IEnumerable<AdvertSummaryDto>> SearchSummariesAsync(
		AdvertSearchQuery? query,
		CancellationToken cancellationToken = default)
	{
		if (query == null)
		{
			return Task.FromResult<IEnumerable<AdvertSummaryDto>>(Summaries);
		}

		IEnumerable<AdvertSummaryDto> result = Summaries;

		if (!string.IsNullOrWhiteSpace(query.Isbn))
		{
			var needle = Normalize(query.Isbn);
			result = result.Where(a =>
				a.Type == CatalogAdvertTypeCodes.Books
				&& a.Isbn is not null
				&& Normalize(a.Isbn).Contains(needle, StringComparison.Ordinal));
		}

		if (!string.IsNullOrWhiteSpace(query.Q))
		{
			var keyword = query.Q.Trim();
			var normalizedIsbnProbe = Normalize(keyword);
			result = result.Where(a =>
				a.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| (a.Type == CatalogAdvertTypeCodes.Books
					&& a.Isbn is not null
					&& Normalize(a.Isbn).Contains(normalizedIsbnProbe, StringComparison.Ordinal)));
		}

		return Task.FromResult<IEnumerable<AdvertSummaryDto>>(result.ToList());
	}

	public Task<AdvertDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
	{
		var entry = Catalog.FirstOrDefault(e => e.Summary.Id == id);
		if (entry == null)
			return Task.FromResult<AdvertDetailDto?>(null);

		var s = entry.Summary;
		return Task.FromResult<AdvertDetailDto?>(new AdvertDetailDto
		{
			Id = s.Id,
			Title = s.Title,
			Type = s.Type,
			Isbn = s.Isbn,
			Category = s.Category,
			Subjects = s.Subjects,
			Grade = s.Grade,
			Price = s.Price,
			Description = entry.Description
		});
	}

	private static IReadOnlyList<CatalogMockEntry> BuildCatalog()
	{
		const string demoDesc = "Données de démonstration (catalogue mock). Non persistées.";

		return new List<CatalogMockEntry>
		{
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("6d4b9d4a-1dd1-4a38-8d68-7af4d9cb3c01"),
				Title = "Exemple annonce 1",
				Price = 12.50m,
				Type = CatalogAdvertTypeCodes.Books,
				Isbn = null,
				Category = "General",
				Subjects = null,
				Grade = null
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("9a2d7d6e-8b4c-4d55-a901-2ec6f6c4d202"),
				Title = "Exemple annonce 2",
				Price = 7.00m,
				Type = CatalogAdvertTypeCodes.Books,
				Isbn = null,
				Category = "General",
				Subjects = null,
				Grade = null
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("3f8e5c9b-2a7e-4f1a-9c3d-5b6e7f8a9c03"),
				Title = "Exemple annonce 3",
				Price = 15.00m,
				Type = CatalogAdvertTypeCodes.Books,
				Isbn = "978-3-16-148410-0",
				Category = "Fiction",
				Subjects = "Mathematics",
				Grade = "Grade 10"
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("c4d8f2a1-6e9b-4c7d-a5f3-1e2d3c4b5a01"),
				Title = "Calculatrice scientifique Casio",
				Price = 42.99m,
				Type = CatalogAdvertTypeCodes.Product,
				Isbn = null,
				Category = "Fournitures",
				Subjects = null,
				Grade = null
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("c4d8f2a1-6e9b-4c7d-a5f3-1e2d3c4b5a02"),
				Title = "Cartable à roulettes bleu marine",
				Price = 59.90m,
				Type = CatalogAdvertTypeCodes.Product,
				Isbn = null,
				Category = "Fournitures",
				Subjects = null,
				Grade = null
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("e7b3c91d-5f44-4a8e-b2c6-9d8e7f6a5b03"),
				Title = "Cours particuliers mathématiques",
				Price = 25.00m,
				Type = CatalogAdvertTypeCodes.Service,
				Isbn = null,
				Category = null,
				Subjects = "Mathématiques",
				Grade = "Collège"
			}, demoDesc),
			new(new AdvertSummaryDto
			{
				Id = Guid.Parse("e7b3c91d-5f44-4a8e-b2c6-9d8e7f6a5b04"),
				Title = "Soutien physique chimie niveau lycée",
				Price = 30.00m,
				Type = CatalogAdvertTypeCodes.Service,
				Isbn = null,
				Category = null,
				Subjects = "Physique-Chimie",
				Grade = "Lycée"
			}, demoDesc)
		};
	}

	private static string Normalize(string? isbnText)
	{
		return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
	}
}
