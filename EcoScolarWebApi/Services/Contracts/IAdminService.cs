using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Users;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoScolarWebApi.Services.Contracts
{
    
    public interface IAdminService
    {
        Task<Result<List<UserResponse>>> GetAllUsers(ClaimsPrincipal user);
        Task<Result<UserResponse>> BanUserToggle(ClaimsPrincipal user, string userId);
    }
}
