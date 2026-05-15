using EcoscolarWebApi.Utils.DTOs;

namespace EcoscolarWebApi.Services
{
    public class FakeAdvertSearchService : IAdvertSearchService
    {
        public Task<IEnumerable<AdvertSummaryDto>> GetSummariesAsync()
        {
            var list = new List<AdvertSummaryDto>
            {
                new AdvertSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Exemple annonce 1",
                    Price = 12.50m
                },
                new AdvertSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Exemple annonce 2",
                    Price = 7.00m
                }
            };

            return Task.FromResult<IEnumerable<AdvertSummaryDto>>(list);
        }
    }
}