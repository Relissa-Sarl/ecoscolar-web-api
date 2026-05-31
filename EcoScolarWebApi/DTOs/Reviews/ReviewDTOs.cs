using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Reviews;

public record ReviewRequestDTO([Required][Range(1, 5)] int Rating, string? Comment);

public record ReviewResponseDTO(int ReviewId, string? Comment, int Rating, DateTime Date, string ReviewerId, string ReviewedId, long TransactionId);
