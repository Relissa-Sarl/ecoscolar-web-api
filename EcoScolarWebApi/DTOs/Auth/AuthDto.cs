using EcoScolarWebApi.DTOs.Users;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Auth;

public record LoginRequestDto(
	[Required(ErrorMessage = "The email is required")]
	[EmailAddress(ErrorMessage = "Invalid email format")] string Email,
	[Required(ErrorMessage = "The password is required")] string Password
);

public record RegisterRequestDto(
	[Required] string Nickname,
	[Required][EmailAddress] string Email,
	[Required] string FirstName,
	[Required] string LastName,
	[Required][MinLength(8)] string Password,
	string PostalCode,
	[Required] string BirthdayDate,
	[Required] List<SpokenLanguageDto> SpokenLanguages
);

public record LoginResponseDto(
	[Required] string TokenType,
	[Required] string AccessToken,
	[Required] int ExpiresIn,
	string? RefreshToken
);