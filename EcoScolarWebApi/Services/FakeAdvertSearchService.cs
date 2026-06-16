using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Services.Contracts;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Mock catalogue search aligned with <see cref="AdvertSearchService"/>:
/// <c>isbn</c> keeps Books rows whose normalized ISBN matches;
/// <c>q</c> keeps rows whose title contains the keyword (any PhysicalItem type)
/// or whose Books ISBN matches the normalized keyword.
/// </summary>
public class FakeAdvertSearchService : IAdvertSearchService
{
	private const int DefaultPage = 1;
	private const int DefaultPageSize = 9;
	private const int MaxPageSize = 50;

	private sealed record CatalogMockEntry(AdvertSummaryDto Summary, string Description);

	private static readonly IReadOnlyList<CatalogMockEntry> Catalog = BuildCatalog();

	private static readonly IReadOnlyList<AdvertSummaryDto> Summaries =
		Catalog.Select(e => e.Summary).ToList();

	public Task<CatalogSummaryPageDto> SearchSummariesAsync(
		AdvertSearchQuery? query,
		CancellationToken cancellationToken = default)
	{
		IEnumerable<AdvertSummaryDto> result = Summaries;

		if (query is not null)
		{
			result = ApplyFilters(result, query);
		}

		var pageSize = NormalizePageSize(query?.PageSize);
		var page = NormalizePage(query?.Page);
		var sorted = ApplySort(result, query?.Sort).ToList();
		var totalItems = sorted.Count;
		var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
		page = Math.Min(page, totalPages);

		var items = sorted
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		return Task.FromResult(new CatalogSummaryPageDto
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalItems = totalItems,
			TotalPages = totalPages
		});
	}

	public Task<AdvertDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
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
				Id = 1,
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
				Id = 2,
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
				Id = 3,
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
				Id = 4,
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
				Id = 5,
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
				Id = 6,
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
				Id = 7,
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

	private static IEnumerable<AdvertSummaryDto> ApplyFilters(IEnumerable<AdvertSummaryDto> result, AdvertSearchQuery query)
	{
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

		if (!string.IsNullOrWhiteSpace(query.Type))
		{
			var type = query.Type.Trim();
			result = result.Where(a => a.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
		}

		if (query.MinPrice.HasValue)
			result = result.Where(a => a.Price >= query.MinPrice.Value);

		if (query.MaxPrice.HasValue)
			result = result.Where(a => a.Price <= query.MaxPrice.Value);

		var categories = SplitTerms(query.Category);
		if (categories.Length > 0)
			result = result.Where(a => a.Category is not null && categories.Contains(a.Category.Trim().ToLowerInvariant()));

		var grades = SplitTerms(query.Grade);
		if (grades.Length > 0)
			result = result.Where(a => a.Grade is not null && grades.Contains(a.Grade.Trim().ToLowerInvariant()));

		var subjects = SplitTerms(query.Subjects);
		if (subjects.Length > 0)
			result = result.Where(a => a.Subjects is not null && subjects.Contains(a.Subjects.Trim().ToLowerInvariant()));

		return result;
	}

	private static IEnumerable<AdvertSummaryDto> ApplySort(IEnumerable<AdvertSummaryDto> result, string? sort)
	{
		return sort?.Trim().ToLowerInvariant() switch
		{
			"price_asc" => result.OrderBy(a => a.Price).ThenByDescending(a => a.Id),
			"price_desc" => result.OrderByDescending(a => a.Price).ThenByDescending(a => a.Id),
			_ => result.OrderByDescending(a => a.Id)
		};
	}

	private static int NormalizePage(int? page)
	{
		return Math.Max(DefaultPage, page ?? DefaultPage);
	}

	private static int NormalizePageSize(int? pageSize)
	{
		return Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
	}

	private static string[] SplitTerms(string? rawTerms)
	{
		return rawTerms?
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(term => term.Trim().ToLowerInvariant())
			.Where(term => term.Length > 0)
			.Distinct()
			.ToArray() ?? [];
	}

	private static string Normalize(string? isbnText)
	{
		return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
	}
}
