using EcoscolarWebApi.Utils.DTOs;

namespace EcoscolarWebApi.Services
{
    public class FakeAdvertSearchService : IAdvertSearchService
    {
        private static readonly IReadOnlyList<AdvertSummaryDto> Summaries =
            new List<AdvertSummaryDto>
            {
                new()
                {
                    Id = Guid.Parse("6d4b9d4a-1dd1-4a38-8d68-7af4d9cb3c01"),
                    Title = "Exemple annonce 1",
                    Price = 12.50m
                },
                new()
                {
                    Id = Guid.Parse("9a2d7d6e-8b4c-4d55-a901-2ec6f6c4d202"),
                    Title = "Exemple annonce 2",
                    Price = 7.00m
                }
            };

        public Task<IEnumerable<AdvertSummaryDto>> GetSummariesAsync()
        {
            return Task.FromResult<IEnumerable<AdvertSummaryDto>>(Summaries);
        }
    }
}
