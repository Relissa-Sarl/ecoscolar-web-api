using EcoscolarWebApi.Utils.DTOs;

namespace EcoscolarWebApi.Services
{
    public interface IAdvertSearchService
    {
        Task<IEnumerable<AdvertSummaryDto>> GetSummariesAsync();
    }
}