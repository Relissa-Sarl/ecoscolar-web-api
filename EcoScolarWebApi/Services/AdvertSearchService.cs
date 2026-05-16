using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Services
{
    /// <summary>
    /// Catalogue summaries/detail backed by EF Core (<see cref="Books"/>). Same contract as <see cref="FakeAdvertSearchService"/>.
    /// <see cref="AdvertSummaryDto.Id"/> is a deterministic <see cref="Guid"/> encoding <see cref="Adverts.AdvertId"/> for stable REST paths until the API settles on long ids.
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
            // Filtres sur IQueryable pour que WHERE / JOIN partent au serveur SQL (voir PR Copilot perf).
            IQueryable<Books> q = _context.Books
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.BookCategory)
                .Include(b => b.Pictures);

            if (query != null && !string.IsNullOrWhiteSpace(query.Isbn))
            {
                var needle = Normalize(query.Isbn);
                q = q.Where(b =>
                    b.ISBN != null
                    && b.ISBN.Trim() != string.Empty
                    && b.ISBN.Replace("-", string.Empty).Trim().ToLower().Contains(needle));
            }

            if (query != null && !string.IsNullOrWhiteSpace(query.Q))
            {
                var keyword = query.Q.Trim();
                var titleProbe = keyword.ToLower();
                var isbnProbe = Normalize(keyword);

                q = q.Where(b =>
                    b.Title.ToLower().Contains(titleProbe)
                    || (b.ISBN != null
                        && b.ISBN.Trim() != string.Empty
                        && b.ISBN.Replace("-", string.Empty).Trim().ToLower().Contains(isbnProbe)));
            }

            var books = await q.ToListAsync(cancellationToken);

            return books.Select(ToSummaryDto).ToList();
        }

        public async Task<AdvertDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!TryAdvertIdFromCatalogGuid(id, out var advertId))
                return null;

            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.BookCategory)
                .Include(b => b.Pictures)
                .FirstOrDefaultAsync(b => b.AdvertId == advertId, cancellationToken);

            return book == null ? null : ToDetailDto(book, id);
        }

        private static AdvertSummaryDto ToSummaryDto(Books b)
        {
            return new AdvertSummaryDto
            {
                Id = CatalogIdFromAdvertId(b.AdvertId),
                Title = b.Title,
                Price = b.Price,
                Type = CatalogAdvertTypeCodes.Book,
                Isbn = string.IsNullOrWhiteSpace(b.ISBN) ? null : b.ISBN,
                Category = b.BookCategory?.Name,
                Subject = null,
                Grade = null
            };
        }

        private static AdvertDetailDto ToDetailDto(Books b, Guid catalogId)
        {
            string? imageUrl = b.Pictures?.FirstOrDefault()?.Label;

            return new AdvertDetailDto
            {
                Id = catalogId,
                Title = b.Title,
                Type = CatalogAdvertTypeCodes.Book,
                Isbn = string.IsNullOrWhiteSpace(b.ISBN) ? null : b.ISBN,
                Category = b.BookCategory?.Name,
                Subject = null,
                Grade = null,
                Price = b.Price,
                Description = b.Description ?? string.Empty,
                ImageUrl = imageUrl
            };
        }

        private static Guid CatalogIdFromAdvertId(long advertId)
        {
            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(), advertId);
            return new Guid(bytes);
        }

        private static bool TryAdvertIdFromCatalogGuid(Guid catalogId, out long advertId)
        {
            var bytes = catalogId.ToByteArray();
            advertId = 0;
            for (var i = 8; i < 16; i++)
            {
                if (bytes[i] != 0)
                    return false;
            }

            advertId = BitConverter.ToInt64(bytes, 0);
            return true;
        }

        private static string Normalize(string? isbnText)
        {
            return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
        }
    }
}
