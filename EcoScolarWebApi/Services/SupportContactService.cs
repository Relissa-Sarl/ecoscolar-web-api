using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Services;

public class SupportContactService(
    EcoscolarDbContext context,
    UserManager<User> userManager,
    IConfiguration configuration,
    ILogger<SupportContactService> logger) : ISupportContactService
{
    private const string SupportWelcomeMessage =
        "Merci pour votre message. Un membre de notre équipe support vous répondra ici dès que possible.";

    public async Task<Result<SupportContactResponseDto>> SubmitAsync(
        SupportContactRequestDto request,
        ClaimsPrincipal? user)
    {
        var email = (request.Email ?? string.Empty).Trim();
        var subject = (request.Subject ?? string.Empty).Trim();
        var message = (request.Message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
            return Result<SupportContactResponseDto>.Failure(
                "Veuillez saisir une adresse e-mail valide.",
                ErrorType.Conflict);

        if (string.IsNullOrWhiteSpace(subject) || subject.Length < 3)
            return Result<SupportContactResponseDto>.Failure(
                "Veuillez saisir l'objet du message.",
                ErrorType.Conflict);

        if (string.IsNullOrWhiteSpace(message) || message.Length < 10)
            return Result<SupportContactResponseDto>.Failure(
                "Veuillez saisir un message.",
                ErrorType.Conflict);

        string? userId = null;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var currentUser = await userManager.GetUserAsync(user);
            userId = currentUser?.Id;
        }

        var createdAt = DateTime.UtcNow;
        var ticket = new SupportTicket
        {
            Email = email,
            Subject = subject,
            Message = message,
            UserId = userId,
            CreatedAt = createdAt
        };

        ticket.Messages.Add(new SupportTicketMessage
        {
            Body = SupportWelcomeMessage,
            IsFromSupport = true,
            CreatedAt = createdAt
        });

        context.SupportTickets.Add(ticket);
        await context.SaveChangesAsync();

        var destination = configuration["Support:DestinationEmail"] ?? "support@ecoscolar.local";
        logger.LogInformation(
            "Support ticket {TicketId} created for {Email}, subject: {Subject}, notify: {Destination}",
            ticket.Id, email, subject, destination);

        return Result<SupportContactResponseDto>.Success(new SupportContactResponseDto(ticket.Id));
    }

    public async Task<Result<IReadOnlyList<SupportTicketSummaryDto>>> GetMyTicketsAsync(ClaimsPrincipal user)
    {
        var currentUser = await ResolveUserAsync(user);
        if (currentUser is null)
            return Result<IReadOnlyList<SupportTicketSummaryDto>>.Failure(
                "Authentication required.",
                ErrorType.Unauthorized);

        var email = currentUser.Email?.Trim();
        var tickets = await context.SupportTickets
            .AsNoTracking()
            .Where(t =>
                t.UserId == currentUser.Id
                || (email != null && t.Email == email))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SupportTicketSummaryDto(
                t.Id,
                t.Email,
                t.Subject,
                t.CreatedAt))
            .ToListAsync();

        return Result<IReadOnlyList<SupportTicketSummaryDto>>.Success(tickets);
    }

    public async Task<Result<SupportTicketDetailDto>> GetMyTicketAsync(ClaimsPrincipal user, int ticketId)
    {
        var currentUser = await ResolveUserAsync(user);
        if (currentUser is null)
            return Result<SupportTicketDetailDto>.Failure(
                "Authentication required.",
                ErrorType.Unauthorized);

        var ticket = await FindAccessibleTicketAsync(currentUser, ticketId);
        if (ticket is null)
            return Result<SupportTicketDetailDto>.Failure(
                "Demande introuvable.",
                ErrorType.NotFound);

        return Result<SupportTicketDetailDto>.Success(new SupportTicketDetailDto(
            ticket.Id,
            ticket.Email,
            ticket.Subject,
            ticket.Message,
            ticket.CreatedAt));
    }

    public async Task<Result<IReadOnlyList<SupportTicketMessageDto>>> GetTicketMessagesAsync(
        ClaimsPrincipal user,
        int ticketId)
    {
        var currentUser = await ResolveUserAsync(user);
        if (currentUser is null)
            return Result<IReadOnlyList<SupportTicketMessageDto>>.Failure(
                "Authentication required.",
                ErrorType.Unauthorized);

        if (!await TicketIsAccessibleAsync(currentUser, ticketId))
            return Result<IReadOnlyList<SupportTicketMessageDto>>.Failure(
                "Demande introuvable.",
                ErrorType.NotFound);

        var messages = await context.SupportTicketMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new SupportTicketMessageDto(
                m.Id,
                m.Body,
                m.IsFromSupport,
                m.CreatedAt))
            .ToListAsync();

        return Result<IReadOnlyList<SupportTicketMessageDto>>.Success(messages);
    }

    public async Task<Result<SupportTicketMessageDto>> AddTicketMessageAsync(
        ClaimsPrincipal user,
        int ticketId,
        SupportTicketMessageRequestDto request)
    {
        var currentUser = await ResolveUserAsync(user);
        if (currentUser is null)
            return Result<SupportTicketMessageDto>.Failure(
                "Authentication required.",
                ErrorType.Unauthorized);

        var body = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(body))
            return Result<SupportTicketMessageDto>.Failure(
                "Veuillez saisir un message.",
                ErrorType.Conflict);

        if (!await TicketIsAccessibleAsync(currentUser, ticketId))
            return Result<SupportTicketMessageDto>.Failure(
                "Demande introuvable.",
                ErrorType.NotFound);

        var message = new SupportTicketMessage
        {
            TicketId = ticketId,
            Body = body,
            IsFromSupport = false,
            CreatedAt = DateTime.UtcNow
        };

        context.SupportTicketMessages.Add(message);
        await context.SaveChangesAsync();

        return Result<SupportTicketMessageDto>.Success(new SupportTicketMessageDto(
            message.Id,
            message.Body,
            message.IsFromSupport,
            message.CreatedAt));
    }

    private async Task<User?> ResolveUserAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return null;

        return await userManager.GetUserAsync(user);
    }

    private async Task<SupportTicket?> FindAccessibleTicketAsync(User currentUser, int ticketId)
    {
        var email = currentUser.Email?.Trim();

        return await context.SupportTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == ticketId
                && (t.UserId == currentUser.Id || (email != null && t.Email == email)));
    }

    private async Task<bool> TicketIsAccessibleAsync(User currentUser, int ticketId)
        => await FindAccessibleTicketAsync(currentUser, ticketId) is not null;
}
