using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Support;
using System.Security.Claims;

namespace EcoScolarWebApi.Services.Contracts;

public interface ISupportContactService
{
    Task<Result<SupportContactResponseDto>> SubmitAsync(
        SupportContactRequestDto request,
        ClaimsPrincipal? user);

    Task<Result<IReadOnlyList<SupportTicketSummaryDto>>> GetMyTicketsAsync(ClaimsPrincipal user);

    Task<Result<SupportTicketDetailDto>> GetMyTicketAsync(ClaimsPrincipal user, int ticketId);

    Task<Result<IReadOnlyList<SupportTicketMessageDto>>> GetTicketMessagesAsync(
        ClaimsPrincipal user,
        int ticketId);

    Task<Result<SupportTicketMessageDto>> AddTicketMessageAsync(
        ClaimsPrincipal user,
        int ticketId,
        SupportTicketMessageRequestDto request);
}
