using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Transactions;

public record DisputeRequestDto
{
    [Required]
    public EcoScolarWebApi.Enums.DisputeReason Reason { get; init; }

    [Required]
    public string Description { get; init; } = string.Empty;
}
