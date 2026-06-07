using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using System.Security.Claims;

namespace EcoScolarWebApi.Services.Contracts
{
    
    public interface IAdminService
    {
        Task<Result<List<UserResponse>>> GetAllUsers(ClaimsPrincipal user);
        Task<Result<UserResponse>> BanUserToggle(ClaimsPrincipal user, string userId);
        Task<Result<List<SupportTicketAdminDto>>> GetAllSupports(ClaimsPrincipal user);
        Task<Result<SupportTicketMessageAdminDto>> AddTicketMessage(ClaimsPrincipal user, int ticketId, SupportTicketMessageRequestDto request);
    }
}
