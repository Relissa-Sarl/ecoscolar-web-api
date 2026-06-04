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
    public async Task<Result<SupportContactResponseDto>> SubmitAsync(
        SupportContactRequestDto request,
        ClaimsPrincipal? user)
    {
        var email = request.Email.Trim();
        var subject = request.Subject.Trim();
        var message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return Result<SupportContactResponseDto>.Failure(
                "Veuillez saisir un message.",
                ErrorType.Invalid);

        string? userId = null;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var currentUser = await userManager.GetUserAsync(user);
            userId = currentUser?.Id;
        }

        var ticket = new SupportTicket
        {
            Email = email,
            Subject = subject,
            Message = message,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        context.SupportTickets.Add(ticket);
        await context.SaveChangesAsync();

        var destination = configuration["Support:DestinationEmail"] ?? "support@ecoscolar.local";
        logger.LogInformation(
            "Support ticket {TicketId} created for {Email}, subject: {Subject}, notify: {Destination}",
            ticket.Id, email, subject, destination);

        return Result<SupportContactResponseDto>.Success(new SupportContactResponseDto(ticket.Id));
    }

    public async Task<Result<IReadOnlyList<SupportTicketReadDto>>> GetMyTicketsAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Result<IReadOnlyList<SupportTicketReadDto>>.Failure(
                "Authentication required.",
                ErrorType.Unauthorized);

        var currentUser = await userManager.GetUserAsync(user);
        if (currentUser is null)
            return Result<IReadOnlyList<SupportTicketReadDto>>.Failure(
                "User not found.",
                ErrorType.NotFound);

        var email = currentUser.Email?.Trim();
        var tickets = await context.SupportTickets
            .AsNoTracking()
            .Where(t =>
                t.UserId == currentUser.Id
                || (email != null && t.Email == email))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SupportTicketReadDto(
                t.Id,
                t.Email,
                t.Subject,
                t.Message,
                t.CreatedAt))
            .ToListAsync();

        return Result<IReadOnlyList<SupportTicketReadDto>>.Success(tickets);
    }
}