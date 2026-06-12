namespace EcoScolarWebApi.DTOs.Adverts;

/// <summary>
/// Query parameters for PhysicalItem search (summary list).
/// </summary>
/// <remarks>
/// For <c>GET api/v1/adverts/summary</c>, filters are applied before pagination.
/// <see cref="Category"/>, <see cref="Subjects"/>, and <see cref="Grade"/> accept one or more
/// comma-separated canonical names from the reference tables.
/// </remarks>
public class AdvertSearchQuery
{
	public string? Q { get; set; }
	public string? Isbn { get; set; }
	public string? Type { get; set; }
	public string? Category { get; set; }
	public decimal? MinPrice { get; set; } = null;
	public decimal? MaxPrice { get; set; }
	public string? Subjects { get; set; }
	public string? Grade { get; set; }
	public string? Sort { get; set; }
	public int? Page { get; set; }
	public int? PageSize { get; set; }
}
