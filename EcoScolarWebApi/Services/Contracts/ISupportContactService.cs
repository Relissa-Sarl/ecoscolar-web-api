using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Support;
using System.Security.Claims;

namespace EcoScolarWebApi.Services.Contracts;

public interface ISupportContactService
{
    Task<Result<SupportContactResponseDto>> SubmitAsync(
        SupportContactRequestDto request,
        ClaimsPrincipal? user);

    Task<Result<IReadOnlyList<SupportTicketReadDto>>> GetMyTicketsAsync(ClaimsPrincipal user);
}