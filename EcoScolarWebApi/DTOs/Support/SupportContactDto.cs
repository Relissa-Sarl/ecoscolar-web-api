using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Support;

public class SupportContactRequestDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Veuillez saisir une adresse e-mail valide.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(5, ErrorMessage = "Veuillez saisir l'objet du message.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(10, ErrorMessage = "Veuillez saisir un message.")]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}

public record SupportContactResponseDto(int Id);

public record SupportTicketSummaryDto(
    int Id,
    string Email,
    string Subject,
    DateTime CreatedAt);

public record SupportTicketDetailDto(
    int Id,
    string Email,
    string Subject,
    string Message,
    DateTime CreatedAt);

public record SupportTicketMessageDto(
    int Id,
    string Body,
    bool IsFromSupport,
    DateTime CreatedAt);

public class SupportTicketMessageRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Veuillez saisir un message.")]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}