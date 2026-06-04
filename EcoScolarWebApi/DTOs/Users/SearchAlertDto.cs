using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.Users;

public record SearchAlertReadDto(
    int Id,
    string? Q,
    string? Isbn,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Subjects,
    string? Grade,
    DateTime CreatedAt
)
{
    public static SearchAlertReadDto FromEntity(SearchAlert entity) => new(
        Id: entity.ResearchId,
        Q: string.IsNullOrWhiteSpace(entity.AdvertSearch) ? null : entity.AdvertSearch,
        Isbn: entity.ISBN,
        Category: entity.BookCategory?.Name,
        MinPrice: null,
        MaxPrice: entity.MaxPrice,
        Subjects: entity.Subject?.Name,
        Grade: null,
        CreatedAt: DateTime.UtcNow
    );
}

public class CreateSearchAlertDto
{
    public string? Q { get; set; }
    public string? Isbn { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Subjects { get; set; }
    public string? Grade { get; set; }

    public bool HasAnyCriterion() =>
        !string.IsNullOrWhiteSpace(Q)
        || !string.IsNullOrWhiteSpace(Isbn)
        || !string.IsNullOrWhiteSpace(Category)
        || !string.IsNullOrWhiteSpace(Subjects)
        || !string.IsNullOrWhiteSpace(Grade)
        || MinPrice.HasValue
        || MaxPrice.HasValue;
}
