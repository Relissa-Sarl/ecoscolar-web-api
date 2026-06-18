using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Enums;
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
        Task<Result<AdvertReadDto>> BlockAdvert(ClaimsPrincipal user, long advertId);
        Task<Result<List<AbuseReportAdminDto>>> GetAllAbuses(ClaimsPrincipal user);
        Task<Result<IEnumerable<FlaggedUserDto>>> GetFlaggedUsers(ClaimsPrincipal user);
        Task<Result<AbuseReportAdminDto>> ChangeAbuseStatus(ClaimsPrincipal user, int abuseId, AbuseStatusRequestDto status);
        Task<Result> DeleteAbuse(ClaimsPrincipal user, int abuseId);
    }
}
