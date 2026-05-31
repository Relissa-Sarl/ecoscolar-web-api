using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Reviews;

// DTO for creating a new review, with validation attributes for rating
public record ReviewRequestDTO([Required][Range(1, 5)] int Rating, string? Comment);

// DTO for returning review details, including reviewer and reviewed user information
public record ReviewResponseDTO(
	int ReviewId,
	string? Comment,
	int Rating,
	DateTime Date,
	string ReviewerId,
	string ReviewerNickname,
	string ReviewedId,
	string ReviewedNickname,
	long TransactionId
	);
