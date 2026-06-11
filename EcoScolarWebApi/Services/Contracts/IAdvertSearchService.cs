using EcoScolarWebApi.DTOs.Adverts;

namespace EcoScolarWebApi.Services.Contracts;

public interface IAdvertSearchService
{
    Task<IEnumerable<AdvertSummaryDto>> SearchSummariesAsync(AdvertSearchQuery? query, CancellationToken cancellationToken = default);
    Task<AdvertDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}