using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using EcoScolarWebApi.Enums;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Catalogue summaries/detail sur <see cref="Advert"/> (livres, produits hors livre, services).
/// Filtre <c>isbn</c> : lignes résolues comme annonces reliées à <see cref="Book"/>.
/// Filtre <c>q</c> : titre ou ISBN (pour les lignes livre uniquement dans la sous-requête).
/// Les autres filtres et la pagination sont appliqués avant le mapping DTO.
/// </summary>
public sealed class AdvertSearchService : IAdvertSearchService
{
	private const int DefaultPage = 1;
	private const int DefaultPageSize = 9;
	private const int MaxPageSize = 50;

	private readonly EcoscolarDbContext _context;

	public AdvertSearchService(EcoscolarDbContext context)
	{
		_context = context;
	}

	public async Task<CatalogSummaryPageDto> SearchSummariesAsync(
		AdvertSearchQuery? query,
		CancellationToken cancellationToken = default)
	{
		IQueryable<Advert> advertsQuery = _context.Adverts
			.AsNoTracking()
			.Where(a => a.Status == AdvertStatus.ACTIVE);

		if (query is not null)
		{
			advertsQuery = ApplyFilters(advertsQuery, query);
		}

		var pageSize = NormalizePageSize(query?.PageSize);
		var page = NormalizePage(query?.Page);
		var totalItems = await advertsQuery.CountAsync(cancellationToken);
		var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
		page = Math.Min(page, totalPages);

		var adverts = await ApplySort(advertsQuery, query?.Sort)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		var bookAdvertIds = adverts.OfType<Book>().Select(b => b.AdvertId).Distinct().ToArray();
		var serviceAdvertIds = adverts.OfType<TutoringAdvert>().Select(s => s.AdvertId).Distinct().ToArray();

		var booksDict = bookAdvertIds.Length == 0
			? []
			: await _context.Books.AsNoTracking()
				.Include(b => b.BookCategory)
				.Include(b => b.Pictures)
				.Where(b => bookAdvertIds.Contains(b.AdvertId))
				.ToDictionaryAsync(b => b.AdvertId, cancellationToken);

		var servicesDict = serviceAdvertIds.Length == 0
			? []
			: await _context.Services.AsNoTracking()
				.Include(s => s.Subject)
				.Include(s => s.SchoolGrade)
				.Where(s => serviceAdvertIds.Contains(s.AdvertId))
				.ToDictionaryAsync(s => s.AdvertId, cancellationToken);

		return new CatalogSummaryPageDto
		{
			Items = adverts.Select(a => MapSummary(a, booksDict, servicesDict)).ToList(),
			Page = page,
			PageSize = pageSize,
			TotalItems = totalItems,
			TotalPages = totalPages
		};
	}

	public async Task<AdvertDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
	{
		var advert = await _context.Adverts
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.AdvertId == id, cancellationToken);

		if (advert is null)
			return null;

		var bookDetail = await _context.Books
			.AsNoTracking()
			.Include(b => b.BookCategory)
			.Include(b => b.Pictures)
			.FirstOrDefaultAsync(b => b.AdvertId == id, cancellationToken);
		if (bookDetail != null)
			return ToDetailFromBook(bookDetail);

		var serviceDetail = await _context.Services
			.AsNoTracking()
			.Include(s => s.Subject)
			.Include(s => s.SchoolGrade)
			.FirstOrDefaultAsync(s => s.AdvertId == id, cancellationToken);
		if (serviceDetail != null)
			return ToDetailFromService(serviceDetail);

		var productDetail = await _context.Products
			.AsNoTracking()
			.Include(p => p.Pictures)
			.Where(p =>
				p.AdvertId == id
				&& !_context.Set<Book>().Any(Books => Books.AdvertId == p.AdvertId))
			.FirstOrDefaultAsync(cancellationToken);

		return productDetail == null ? null : ToDetailFromPhysical(productDetail);
	}

	private IQueryable<Advert> ApplyFilters(IQueryable<Advert> advertsQuery, AdvertSearchQuery query)
	{
		if (!string.IsNullOrWhiteSpace(query.Isbn))
		{
			var needle = Normalize(query.Isbn);

			advertsQuery = advertsQuery.Where(a =>
				_context.Set<Book>().Any(b =>
					b.AdvertId == a.AdvertId
					&& b.ISBN != null
					&& b.ISBN.Trim() != string.Empty
					&& b.ISBN.Replace("-", string.Empty).Trim().ToLower().Contains(needle)));
		}

		if (!string.IsNullOrWhiteSpace(query.Q))
		{
			var keyword = query.Q.Trim();
			var titleProbe = keyword.ToLower();
			var isbnProbe = Normalize(keyword);

			advertsQuery = advertsQuery.Where(a =>
				a.Title.ToLower().Contains(titleProbe)
				|| _context.Set<Book>().Any(b =>
					b.AdvertId == a.AdvertId
					&& b.ISBN != null
					&& b.ISBN.Trim() != string.Empty
					&& b.ISBN.Replace("-", string.Empty).Trim().ToLower().Contains(isbnProbe)));
		}

		advertsQuery = ApplyTypeFilter(advertsQuery, query.Type);

		if (query.MinPrice.HasValue)
			advertsQuery = advertsQuery.Where(a => a.Price >= query.MinPrice.Value);

		if (query.MaxPrice.HasValue)
			advertsQuery = advertsQuery.Where(a => a.Price <= query.MaxPrice.Value);

		var categories = SplitTerms(query.Category);
		if (categories.Length > 0)
		{
			advertsQuery = advertsQuery.Where(a =>
				_context.Set<Book>().Any(b =>
					b.AdvertId == a.AdvertId
					&& b.BookCategory != null
					&& categories.Contains(b.BookCategory.Name.ToLower())));
		}

		var grades = SplitTerms(query.Grade);
		if (grades.Length > 0)
		{
			advertsQuery = advertsQuery.Where(a =>
				_context.Set<TutoringAdvert>().Any(s =>
					s.AdvertId == a.AdvertId
					&& s.SchoolGrade != null
					&& grades.Contains(s.SchoolGrade.Name.ToLower())));
		}

		var subjects = SplitTerms(query.Subjects);
		if (subjects.Length > 0)
		{
			advertsQuery = advertsQuery.Where(a =>
				_context.Set<TutoringAdvert>().Any(s =>
					s.AdvertId == a.AdvertId
					&& s.Subject != null
					&& subjects.Contains(s.Subject.Name.ToLower())));
		}

		return advertsQuery;
	}

	private static IQueryable<Advert> ApplyTypeFilter(IQueryable<Advert> advertsQuery, string? type)
	{
		return type?.Trim().ToUpperInvariant() switch
		{
			CatalogAdvertTypeCodes.Books => advertsQuery.Where(a => a is Book),
			CatalogAdvertTypeCodes.Product => advertsQuery.Where(a => a is PhysicalItem && !(a is Book)),
			CatalogAdvertTypeCodes.Service => advertsQuery.Where(a => a is TutoringAdvert),
			_ => advertsQuery
		};
	}

	private static IQueryable<Advert> ApplySort(IQueryable<Advert> advertsQuery, string? sort)
	{
		return sort?.Trim().ToLowerInvariant() switch
		{
			"price_asc" => advertsQuery
				.OrderBy(a => a.Price)
				.ThenByDescending(a => a.CreatedAt)
				.ThenByDescending(a => a.AdvertId),
			"price_desc" => advertsQuery
				.OrderByDescending(a => a.Price)
				.ThenByDescending(a => a.CreatedAt)
				.ThenByDescending(a => a.AdvertId),
			_ => advertsQuery
				.OrderByDescending(a => a.CreatedAt)
				.ThenByDescending(a => a.AdvertId)
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

	private static AdvertSummaryDto MapSummary(
		Advert a,
		Dictionary<long, Book> booksDict,
		Dictionary<long, TutoringAdvert> servicesDict)
	{
		switch (a)
		{
			case Book bk:
				{
					booksDict.TryGetValue(bk.AdvertId, out var fullBk);
					var src = fullBk ?? bk;
					return new AdvertSummaryDto
					{
						Id = bk.AdvertId,
						Title = bk.Title,
						Price = bk.Price,
						Type = CatalogAdvertTypeCodes.Books,
						Isbn = string.IsNullOrWhiteSpace(src.ISBN) ? null : src.ISBN,
						Category = src.BookCategory?.Name,
						Subjects = null,
						Grade = null,
						sellerId = bk.SellerId
					};
				}
			case TutoringAdvert svc:
				{
					servicesDict.TryGetValue(svc.AdvertId, out var fullSvc);
					var src = fullSvc ?? svc;
					return new AdvertSummaryDto
					{
						Id = svc.AdvertId,
						Title = svc.Title,
						Price = svc.Price,
						Type = CatalogAdvertTypeCodes.Service,
						Isbn = null,
						Category = null,
						Subjects = src.Subject?.Name,
						Grade = src.SchoolGrade?.Name,
						sellerId = svc.SellerId
					};
				}
			case PhysicalItem phy when phy is not Book:
				return new AdvertSummaryDto
				{
					Id = phy.AdvertId,
					Title = phy.Title,
					Price = phy.Price,
					Type = CatalogAdvertTypeCodes.Product,
					Isbn = null,
					Category = null,
					Subjects = null,
					Grade = null,
					sellerId = phy.SellerId
				};
			default:
				throw new InvalidOperationException($"Unknown PhysicalItem CLR type '{a.GetType().Name}'.");
		}
	}

	private static AdvertDetailDto ToDetailFromBook(Book b)
	{
		string? imageUrl = b.Pictures?.FirstOrDefault()?.Label;

		return new AdvertDetailDto
		{
			Id = b.AdvertId,
			Title = b.Title,
			Type = CatalogAdvertTypeCodes.Books,
			Isbn = string.IsNullOrWhiteSpace(b.ISBN) ? null : b.ISBN,
			Category = b.BookCategory?.Name,
			Subjects = null,
			Grade = null,
			Price = b.Price,
			Description = b.Description ?? string.Empty,
			ImageUrl = imageUrl,
			sellerId = b.SellerId
		};
	}

	private static AdvertDetailDto ToDetailFromService(TutoringAdvert s)
	{
		return new AdvertDetailDto
		{
			Id = s.AdvertId,
			Title = s.Title,
			Type = CatalogAdvertTypeCodes.Service,
			Isbn = null,
			Category = null,
			Subjects = s.Subject?.Name,
			Grade = s.SchoolGrade?.Name,
			Price = s.Price,
			Description = s.Description ?? string.Empty,
			ImageUrl = null,
			sellerId = s.SellerId
		};
	}

	private static AdvertDetailDto ToDetailFromPhysical(PhysicalItem p)
	{
		string? imageUrl = p.Pictures?.FirstOrDefault()?.Label;
		return new AdvertDetailDto
		{
			Id = p.AdvertId,
			Title = p.Title,
			Type = CatalogAdvertTypeCodes.Product,
			Isbn = null,
			Category = null,
			Subjects = null,
			Grade = null,
			Price = p.Price,
			Description = p.Description ?? string.Empty,
			ImageUrl = imageUrl,
			sellerId = p.SellerId
		};
	}

	private static string Normalize(string? isbnText)
	{
		return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
	}
}
