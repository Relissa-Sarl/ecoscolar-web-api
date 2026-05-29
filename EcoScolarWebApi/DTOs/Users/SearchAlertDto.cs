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
    public static SearchAlertReadDto FromEntity(UserSearchAlert entity) => new(
        Id: entity.Id,
        Q: entity.Q,
        Isbn: entity.Isbn,
        Category: entity.Category,
        MinPrice: entity.MinPrice,
        MaxPrice: entity.MaxPrice,
        Subjects: entity.Subjects,
        Grade: entity.Grade,
        CreatedAt: entity.CreatedAt
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
