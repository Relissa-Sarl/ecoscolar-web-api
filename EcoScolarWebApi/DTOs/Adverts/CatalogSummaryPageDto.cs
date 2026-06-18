namespace EcoScolarWebApi.DTOs.Adverts;

public record CatalogSummaryPageDto
{
    public IReadOnlyList<AdvertSummaryDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}
