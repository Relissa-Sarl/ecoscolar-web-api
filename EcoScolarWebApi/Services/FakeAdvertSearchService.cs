using EcoscolarWebApi.Utils.DTOs;

namespace EcoscolarWebApi.Services
{
    /// <summary>
    /// Mock search (T5-1): <c>Isbn</c> and <c>Q</c> filters apply to <see cref="AdvertKind.Book"/> only.
    /// Product/service filters come in T5-2 / T5-3.
    /// </summary>
    public class FakeAdvertSearchService : IAdvertSearchService
    {
        private static readonly IReadOnlyList<AdvertSummaryDto> Summaries =
            new List<AdvertSummaryDto>
            {
                new()
                {
                    Id = Guid.Parse("6d4b9d4a-1dd1-4a38-8d68-7af4d9cb3c01"),
                    Kind = AdvertKind.Book,
                    Title = "Exemple annonce 1",
                    Price = 12.50m
                },
                new()
                {
                    Id = Guid.Parse("9a2d7d6e-8b4c-4d55-a901-2ec6f6c4d202"),
                    Kind = AdvertKind.Book,
                    Title = "Exemple annonce 2",
                    Price = 7.00m
                },
                new()
                {
                    Id = Guid.Parse("3f8e5c9b-2a7e-4f1a-9c3d-5b6e7f8a9c03"),
                    Title = "Exemple annonce 3",
                    Kind = AdvertKind.Book,
                    Isbn = "978-3-16-148410-0",
                    Category = "Fiction",
                    Subject = "Mathematics",
                    Grade = "Grade 10",
                    Price = 15.00m
                }
            };

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
                    a.Kind == AdvertKind.Book
                    && a.Isbn is not null
                    && Normalize(a.Isbn).Contains(needle, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var keyword = query.Q.Trim();
                result = result.Where(a =>
                    a.Kind == AdvertKind.Book
                    && (a.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || (a.Isbn is not null
                            && a.Isbn.Contains(keyword, StringComparison.OrdinalIgnoreCase))));
            }

            return Task.FromResult<IEnumerable<AdvertSummaryDto>>(result.ToList());
        }

        public Task<AdvertDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var summary = Summaries.FirstOrDefault(s => s.Id == id);
            if (summary == null)
                return Task.FromResult<AdvertDetailDto?>(null);

            return Task.FromResult<AdvertDetailDto?>(new AdvertDetailDto
            {
                Id = summary.Id,
                Title = summary.Title,
                Kind = summary.Kind,
                Isbn = summary.Isbn,
                Category = summary.Category,
                Subject = summary.Subject,
                Grade = summary.Grade,
                Price = summary.Price
            });
        }

        private static string Normalize(string? isbnText)
        {
            return isbnText?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? string.Empty;
        }
    }
}
