using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Catalogue summaries/detail sur <see cref="Advert"/> (livres, produits hors livre, services).
/// Filtre <c>isbn</c> : lignes résolues comme annonces reliées à <see cref="Book"/>.
/// Filtre <c>q</c> : titre ou ISBN (pour les lignes livre uniquement dans la sous-requête).
/// </summary>
public sealed class AdvertSearchService : IAdvertSearchService
{
	private readonly EcoscolarDbContext _context;

	public AdvertSearchService(EcoscolarDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<AdvertSummaryDto>> SearchSummariesAsync(
		AdvertSearchQuery? query,
		CancellationToken cancellationToken = default)
	{
		IQueryable<Advert> advertsQuery = _context.Adverts.AsNoTracking();

		if (query != null && !string.IsNullOrWhiteSpace(query.Isbn))
		{
			var needle = Normalize(query.Isbn);

			advertsQuery = advertsQuery.Where(a =>
				_context.Set<Book>().Any(b =>
					b.AdvertId == a.AdvertId
					&& b.ISBN != null
					&& b.ISBN.Trim() != string.Empty
					&& b.ISBN.Replace("-", string.Empty).Trim().ToLower().Contains(needle)));
		}

		if (query != null && !string.IsNullOrWhiteSpace(query.Q))
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

		var adverts = await advertsQuery.ToListAsync(cancellationToken);

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

		return adverts.Select(a => MapSummary(a, booksDict, servicesDict)).ToList();
	}

	public async Task<AdvertDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
	{
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
						Grade = null
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
						Grade = src.SchoolGrade?.Name
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
					Grade = null
				};
			default:
				throw new InvalidOperationException($"Unknown Adverts CLR type '{a.GetType().Name}'.");
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
			ImageUrl = imageUrl
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
			ImageUrl = null
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
			ImageUrl = imageUrl
		};
	}

	private static string Normalize(string? isbnText)
	{
		return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
	}
}
