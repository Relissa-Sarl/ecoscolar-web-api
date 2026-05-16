using EcoscolarWebApi.Utils.DTOs;

namespace EcoscolarWebApi.Services
{
    public interface IAdvertSearchService
    {
        Task<IEnumerable<AdvertSummaryDto>> SearchSummariesAsync(AdvertSearchQuery? query, CancellationToken cancellationToken = default);
        Task<AdvertDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    }
}