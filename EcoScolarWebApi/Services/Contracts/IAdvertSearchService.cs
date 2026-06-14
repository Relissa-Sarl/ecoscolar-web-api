using EcoScolarWebApi.DTOs.Adverts;

namespace EcoScolarWebApi.Services.Contracts;

public interface IAdvertSearchService
{
    Task<CatalogSummaryPageDto> SearchSummariesAsync(AdvertSearchQuery? query, CancellationToken cancellationToken = default);
    Task<AdvertDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}