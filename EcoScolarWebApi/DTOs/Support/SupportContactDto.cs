using EcoScolarWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Support;

public class SupportContactRequestDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Veuillez saisir une adresse e-mail valide.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3, ErrorMessage = "Veuillez saisir l'objet du message.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(10, ErrorMessage = "Veuillez saisir un message.")]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}

public record SupportContactResponseDto(int Id);

public record SupportTicketAdminDto(
    int Id,
    string Email,
    string Subject,
    string Message,
    string? UserId,
    DateTime CreatedAt,
    UserAdminDto? User,
    List<SupportTicketMessageAdminDto> Messages);

public record UserAdminDto(
    string? FirstName,
    string? LastName,
    string? Nickname,
    string? Email);

public record SupportTicketMessageAdminDto(
    int Id,
    string Body,
    bool IsFromSupport,
    DateTime CreatedAt);

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