using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.Users;

public record SearchAlertReadDto(
    int Id,
    string? Q,
    string? AdvertType,
    string? Isbn,
    long? BookCategoryId,
    string? BookCategory,
    long? ProductCategoryId,
    string? ProductCategory,
    long? SubjectId,
    string? Subject,
    long? SchoolGradeId,
    string? Grade,
    decimal? MinPrice,
    decimal? MaxPrice,
    int MatchedCount,
    DateTime CreatedAt
)
{
    public static SearchAlertReadDto FromEntity(SearchAlert entity, int matchedCount = 0) => new(
        Id: entity.ResearchId,
        Q: string.IsNullOrWhiteSpace(entity.AdvertSearch) ? null : entity.AdvertSearch,
        AdvertType: entity.AdvertType,
        Isbn: entity.ISBN,
        BookCategoryId: entity.BookCategoryId,
        BookCategory: entity.BookCategory?.Name,
        ProductCategoryId: entity.ProductCategoryId,
        ProductCategory: entity.ProductCategory?.Name,
        SubjectId: entity.SubjectId,
        Subject: entity.Subject?.Name,
        SchoolGradeId: entity.SchoolGradeId,
        Grade: entity.SchoolGrade?.Name,
        MinPrice: entity.MinPrice,
        MaxPrice: entity.MaxPrice,
        MatchedCount: matchedCount,
        CreatedAt: entity.CreatedAt
    );
}

public class CreateSearchAlertDto
{
    public string? Q { get; set; }
    public string? AdvertType { get; set; }
    public string? Isbn { get; set; }
    public long? BookCategoryId { get; set; }
    public long? ProductCategoryId { get; set; }
    public long? SubjectId { get; set; }
    public long? SchoolGradeId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public bool HasAnyCriterion() =>
        !string.IsNullOrWhiteSpace(Q)
        || !string.IsNullOrWhiteSpace(Isbn)
        || BookCategoryId.HasValue
        || ProductCategoryId.HasValue
        || SubjectId.HasValue
        || SchoolGradeId.HasValue
        || MinPrice.HasValue
        || MaxPrice.HasValue;
}
