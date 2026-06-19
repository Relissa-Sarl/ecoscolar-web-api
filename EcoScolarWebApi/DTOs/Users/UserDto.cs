using EcoScolarWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Users;


public record UserRequest(
    [Required] string Nickname,
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string Email,
    [Required] string PostalCode,
    [Required] string BirthdayDate,
    //[Required] int CurrentSchoolLevelId,
    [Required] IEnumerable<SpokenLanguageDto> Languages
);

public record UserResponse(
    string Id,
    string? Nickname,
    string? FirstName,
    string? LastName,
    string Email,
    bool IsOnboarded,
    bool IsBanned,
    IEnumerable<SpokenLanguageDto> Languages,
    LocationReadDto? Location = null!,
    string? BirthdayDate = null!,
    string[] Roles = null!,
    int BadReviewsCount = 0,
    bool AlerteTooBadReviews = false
//double GlobalRating,
//bool IsBanned,
//int CurrentSchoolLevelId,
//string? StripeAccountId,
//bool IsStripeOnboarded
);

public record UserReadDto(
    string Id,
    string Nickname,
    string FirstName,
    string LastName,
    string Email,
    LocationReadDto? Location,
    string BirthdayDate,
    bool IsOnboarded,
    IEnumerable<SpokenLanguageDto> Languages
//double GlobalRating,
//bool IsBanned,
//int CurrentSchoolLevelId,
//string? StripeAccountId,
//bool IsStripeOnboarded
)
{
    /// <summary>
    /// Creates a new UserReadDto instance from the specified Seller entity.
    /// </summary>
    /// <param name="entity">The Seller entity to convert. Cannot be null.</param>
    /// <returns>A UserReadDto populated with data from the specified Seller entity.</returns>
    public static UserReadDto FromEntity(User entity) => new(
        Id: entity.Id,
        Nickname: entity.Nickname ?? string.Empty,
        FirstName: entity.FirstName ?? string.Empty,
        LastName: entity.LastName ?? string.Empty,
        Email: entity.Email ?? string.Empty,
        BirthdayDate: entity.DateOfBirth ?? string.Empty,
        IsOnboarded: entity.IsOnboarded,
        Location: entity.Location != null ? LocationReadDto.FromEntity(entity.Location) : null,
        Languages: entity.Languages != null
            ? entity.Languages.Select(ul => new SpokenLanguageDto(Label: ul.Label, LanguageLevel: ul.LanguageLevel))
            : Array.Empty<SpokenLanguageDto>()
    //GlobalRating: 0,
    //IsBanned: false,
    //CurrentSchoolLevelId: 0,
    //StripeAccountId: null,
    //IsStripeOnboarded: false
    );
}

/// <summary>
/// Represents a spoken language and the proficiency level for a user or entity.
/// </summary>
/// <param name="Label">The ISO code or name of the spoken language. Cannot be null.</param>
/// <param name="LanguageLevel">The proficiency level of the spoken language (for example, "Native", "Fluent", or "Beginner"). Cannot be null.</param>
public record SpokenLanguageDto(
    [Required] string Label,
    [Required] string LanguageLevel
);

public record UserUpdateDto(
    [Required] string Nickname,
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string PostalCode,
    [Required] string BirthdayDate,
    //[Required] int CurrentSchoolLevelId,
    [Required] IEnumerable<SpokenLanguageDto> Languages
);

public record UserPublicReadDto(
    [Required] string Id,
    [Required] string Nickname
//[Required] int GlobalRating
)
{
    public static UserPublicReadDto FromEntity(User user) => new UserPublicReadDto(user.Id, user.Nickname ?? string.Empty);
}
public record LocationReadDto(
string PostalCode,
string City,
string Region
)
{
    public static LocationReadDto? FromEntity(Location location)
    {
        if (location == null)
        {
            return null;
        }

        return new LocationReadDto(
            PostalCode: location.PostalCode,
            City: location.City,
            Region: location.Region
        );
    }
}