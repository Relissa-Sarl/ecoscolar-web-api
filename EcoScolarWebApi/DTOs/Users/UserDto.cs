using EcoScolarWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Users;

/// <summary>
/// Represents a data transfer object containing user profile information for read operations.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Nickname">The display name or nickname of the user.</param>
/// <param name="FirstName">The first name of the user.</param>
/// <param name="LastName">The last name of the user.</param>
/// <param name="Email">The email address associated with the user.</param>
/// <param name="PostalCode">The postal code of the user's location.</param>
/// <param name="BirthdayDate">The user's date of birth, formatted as a string.</param>
/// <param name="SpokenLanguages">A collection of spoken languages and proficiency levels associated with the user.</param>
public record UserReadDto(
	string Id,
	string Nickname,
	string FirstName,
	string LastName,
	string Email,
	LocationReadDto? Location,
	string BirthdayDate,
	bool IsOnboarded,
	//double GlobalRating,
	//bool IsBanned,
	//int CurrentSchoolLevelId,
	IEnumerable<SpokenLanguageDto> SpokenLanguages
//string? StripeAccountId,
//bool IsStripeOnboarded
)
{
	/// <summary>
	/// Creates a new UserReadDto instance from the specified Seller entity.
	/// </summary>
	/// <param name="entity">The Seller entity to convert. Cannot be null.</param>
	/// <returns>A UserReadDto populated with data from the specified Seller entity.</returns>
	public static UserReadDto FromEntity(User entity) => new	(
		Id: entity.Id,
		Nickname: entity.Nickname ?? "",
		FirstName: entity.FirstName,
		LastName: entity.LastName,
		Email: entity.Email ?? "",
		BirthdayDate: entity.DateOfBirth,
		IsOnboarded: entity.IsOnboarded,
		Location: LocationReadDto.FromEntity(entity.Location) ?? null,
		//GlobalRating: 0,
		//IsBanned: false,
		//CurrentSchoolLevelId: 0,
		SpokenLanguages: entity.Languages != null
			? entity.Languages.Select(ul => new SpokenLanguageDto(Language: ul.Label, Level: ul.LanguageLevel))
			: Array.Empty<SpokenLanguageDto>()
	//StripeAccountId: null,
	//IsStripeOnboarded: false
	);
}

/// <summary>
/// Represents a spoken language and the proficiency level for a user or entity.
/// </summary>
/// <param name="Language">The ISO code or name of the spoken language. Cannot be null.</param>
/// <param name="Level">The proficiency level of the spoken language (for example, "Native", "Fluent", or "Beginner"). Cannot be null.</param>
public record SpokenLanguageDto(
	[Required] string Language,
	[Required] string Level
);

public record UserUpdateDto(
	[Required] string Nickname,
	[Required] string FirstName,
	[Required] string LastName,
	[Required] string PostalCode,
	[Required] string BirthdayDate,
	//[Required] int CurrentSchoolLevelId,
	[Required] IEnumerable<SpokenLanguageDto> SpokenLanguages
);

public record UserPublicReadDto(
	[Required] string Id,
	[Required] string Nickname
//[Required] int GlobalRating
)
{
	public static UserPublicReadDto fromEntity(User user) => new UserPublicReadDto(user.Id, user.Nickname);
}
public class UserDto
{
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
}
public record LocationReadDto(
[Required] string PostalCode,
[Required] string City,
[Required] string Region
)
{
	public static LocationReadDto? FromEntity(Location location)
	{
		if (location != null)
			return new LocationReadDto(
				PostalCode: location.PostalCode,
				City: location.City,
				Region: location.Region
			);

		return null;
	}
}
