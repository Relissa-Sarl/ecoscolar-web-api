using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using EcoscolarWebApi.Utils.Enums;

namespace EcoscolarWebApi.Services
{
    /// <summary>
    /// Mock search (T5-1): <c>Isbn</c> and <c>Q</c> filters apply to <see cref="CatalogAdvertTypeCodes.Book"/> adverts only.
    /// Seed data uses domain <see cref="Books"/> (inherits <see cref="Adverts"/>) then maps to API DTOs.
    /// Product/service filters come in T5-2 / T5-3.
    /// </summary>
    public class FakeAdvertSearchService : IAdvertSearchService
    {
        private sealed record CatalogBookSeed(Books Book, Guid CatalogId, string? Subject, string? Grade);

        /// <remarks>
        /// <see cref="Guid"/> preserves stable mock catalogue IDs for REST paths; EF uses <see cref="Adverts.AdvertId"/> independently.
        /// </remarks>
        private static readonly IReadOnlyList<CatalogBookSeed> CatalogSeed = BuildCatalogSeed();

        private static readonly IReadOnlyList<AdvertSummaryDto> Summaries =
            CatalogSeed.Select(ToSummaryDto).ToList();

        public Task<IEnumerable<AdvertSummaryDto>> SearchSummariesAsync(
            AdvertSearchQuery? query,
            CancellationToken cancellationToken = default)
        {
            // Étape B : aucun critère dans l'URL → toute la liste
            if (query == null)
            {
                return Task.FromResult<IEnumerable<AdvertSummaryDto>>(Summaries);
            }

            // Point de départ pour les filtres (étapes C, D — T5-1 puis T5-2/T5-3)
            IEnumerable<AdvertSummaryDto> result = Summaries;

            if (!string.IsNullOrWhiteSpace(query.Isbn))
            {
                var needle = Normalize(query.Isbn);
                result = result.Where(a =>
                    a.Type == CatalogAdvertTypeCodes.Book
                    && a.Isbn is not null
                    && Normalize(a.Isbn).Contains(needle, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var keyword = query.Q.Trim();
                var normalizedIsbnProbe = Normalize(keyword);
                result = result.Where(a =>
                    a.Type == CatalogAdvertTypeCodes.Book
                    && (a.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || (a.Isbn is not null
                            && Normalize(a.Isbn).Contains(normalizedIsbnProbe, StringComparison.Ordinal))));
            }

            return Task.FromResult<IEnumerable<AdvertSummaryDto>>(result.ToList());
        }

        public Task<AdvertDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var seed = CatalogSeed.FirstOrDefault(s => s.CatalogId == id);
            if (seed == null)
                return Task.FromResult<AdvertDetailDto?>(null);

            var s = ToSummaryDto(seed);
            var b = seed.Book;

            return Task.FromResult<AdvertDetailDto?>(new AdvertDetailDto
            {
                Id = s.Id,
                Title = s.Title,
                Type = s.Type,
                Isbn = s.Isbn,
                Category = s.Category,
                Subject = s.Subject,
                Grade = s.Grade,
                Price = s.Price,
                Description = b.Description ?? string.Empty
            });
        }

        private static IReadOnlyList<CatalogBookSeed> BuildCatalogSeed()
        {
            var categoryGeneral = new BookCategories
            {
                BookCategoryId = 1,
                Name = "General",
                Description = "Mock category (catalogue démo)."
            };

            var categoryFiction = new BookCategories
            {
                BookCategoryId = 2,
                Name = "Fiction",
                Description = "Fiction mock (catalogue démo)."
            };

            const string mockUserId = "00000000-0000-4000-8000-000000000099";
            var catalogUser = new User
            {
                Id = mockUserId,
                UserName = "catalog-mock-user",
                Email = "catalog-mock@ecoscolar.invalid"
            };

            return new List<CatalogBookSeed>
            {
                new(SeedMockBook(
                        catalogUser,
                        advertId: 101,
                        "Exemple annonce 1",
                        12.50m,
                        isbnForEntity: "",
                        categoryGeneral),
                    Guid.Parse("6d4b9d4a-1dd1-4a38-8d68-7af4d9cb3c01"),
                    Subject: null,
                    Grade: null),
                new(SeedMockBook(
                        catalogUser,
                        advertId: 102,
                        "Exemple annonce 2",
                        7.00m,
                        isbnForEntity: "",
                        categoryGeneral),
                    Guid.Parse("9a2d7d6e-8b4c-4d55-a901-2ec6f6c4d202"),
                    Subject: null,
                    Grade: null),
                new(SeedMockBook(
                        catalogUser,
                        advertId: 103,
                        "Exemple annonce 3",
                        15.00m,
                        isbnForEntity: "978-3-16-148410-0",
                        categoryFiction),
                    Guid.Parse("3f8e5c9b-2a7e-4f1a-9c3d-5b6e7f8a9c03"),
                    Subject: "Mathematics",
                    Grade: "Grade 10")
            };
        }

        /// <summary>Build a non-persisted <see cref="Books"/> graph for catalogue mock only.</summary>
        private static Books SeedMockBook(
            User seller,
            long advertId,
            string title,
            decimal price,
            string isbnForEntity,
            BookCategories category)
        {
            var now = DateTime.UtcNow;
            var book = new Books
            {
                AdvertId = advertId,
                Title = title,
                Description = "Données de démonstration (catalogue mock). Non persistées.",
                Price = price,
                CreatedAt = now,
                NotificationDate = now.AddMonths(3),
                Status = AdvertStatus.ACTIVE,
                UserId = seller.Id ?? string.Empty,
                User = seller,
                Condition = Condition.NEW,
                Pictures = new List<Pictures>(),
                ISBN = isbnForEntity,
                Author = "—",
                Publisher = "—",
                Edition = "—",
                WrittenLanguage = Language.FR,
                BookCategoryId = category.BookCategoryId,
                BookCategory = category
            };

            return book;
        }

        private static AdvertSummaryDto ToSummaryDto(CatalogBookSeed seed)
        {
            var b = seed.Book;
            return new AdvertSummaryDto
            {
                Id = seed.CatalogId,
                Title = b.Title,
                Price = b.Price,
                Type = CatalogAdvertTypeCodes.Book,
                Isbn = string.IsNullOrWhiteSpace(b.ISBN) ? null : b.ISBN,
                Category = b.BookCategory?.Name,
                Subject = seed.Subject,
                Grade = seed.Grade
            };
        }

        private static string Normalize(string? isbnText)
        {
            return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
        }
    }
}
