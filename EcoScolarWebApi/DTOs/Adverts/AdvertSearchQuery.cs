namespace EcoScolarWebApi.DTOs.Adverts;

/// <summary>
/// Query parameters for PhysicalItem search (summary list).
/// </summary>
/// <remarks>
/// For <c>GET api/v1/adverts/summary</c>, filters are applied before pagination.
/// Prefer the ID filters for reference data. <see cref="Category"/>, <see cref="Subjects"/>, and
/// <see cref="Grade"/> remain supported as comma-separated canonical names.
/// </remarks>
public class AdvertSearchQuery
{
    public string? Q { get; set; }
    public string? Isbn { get; set; }
    public string? Type { get; set; }
    public string? BookCategoryIds { get; set; }
    public string? SchoolGradeIds { get; set; }
    public string? SubjectIds { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; } = null;
    public decimal? MaxPrice { get; set; }
    public string? Subjects { get; set; }
    public string? Grade { get; set; }
    public string? Sort { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
