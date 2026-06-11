using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Transactions;

public record DisputeRequestDto
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}
