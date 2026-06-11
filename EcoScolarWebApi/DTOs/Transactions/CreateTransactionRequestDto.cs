using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Transactions;

public record CreateTransactionRequestDto
{
    [Required]
    public List<long> AdvertIds { get; init; } = [];

    public string? StripeSessionId { get; init; }
}
