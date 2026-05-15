using EcoscolarWebApi.Services;
using EcoscolarWebApi.Utils.DTOs;
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
        /// Liste / recherche d'annonces (résumés). GET api/v1/adverts?q=...&amp;minPrice=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchSummaries(
            [FromQuery] AdvertSearchQuery? query,
            CancellationToken cancellationToken = default)
        {
            var items = await _advertSearchService.SearchSummariesAsync(query, cancellationToken);
            return Ok(items);
        }

        /// <summary>Détail d'une annonce. GET api/v1/adverts/{id}</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken = default)
        {
            var detail = await _advertSearchService.GetDetailAsync(id, cancellationToken);
            if (detail is null)
            {
                return NotFound();
            }

            return Ok(detail);
        }
    }
}
