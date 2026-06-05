using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
	private readonly EcoscolarDbContext _context;
	private readonly LocationMapper _mapper;

	public LocationsController(EcoscolarDbContext context)
	{
		_context = context;
		_mapper = new LocationMapper();
	}

	[HttpGet("search")]
	public async Task<ActionResult<IEnumerable<LocationResponseDto>>> SearchLocations([FromQuery] string query)
	{
		// Return an empty list if the query is null, empty, or too short to prevent unnecessary database queries
		if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
			return Ok(Enumerable.Empty<LocationResponseDto>());

		var searchTerm = query.Trim().ToLower();

		var efQuery = _context.Locations
			.Where(l => l.PostalCode.StartsWith(searchTerm) || l.City.ToLower().Contains(searchTerm))
			.OrderBy(l => l.PostalCode)
			.Take(15);

		var locations = await _mapper.ProjectToLocationResponseDto(efQuery).ToListAsync();

		return Ok(locations);
	}
}
