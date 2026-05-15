using EcoscolarWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    /// <summary>
    /// Entry point for adverts-related API endpoints. It provides an endpoint to retrieve a list of advert summaries for the catalog.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdvertsController : ControllerBase
    {
        private readonly IAdvertSearchService _advertSearchService;

        public AdvertsController(IAdvertSearchService advertSearchService)
        {
            _advertSearchService = advertSearchService;
        }

        /// <summary>
        /// Retrieves a list of advert summaries for the catalog.
        /// GET api/v1/adverts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSummaries(CancellationToken cancellationToken = default)
        {
            var items = await _advertSearchService.GetSummariesAsync();
            return Ok(items);
        }
    }
}
