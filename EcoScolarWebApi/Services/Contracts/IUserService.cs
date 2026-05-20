using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Users;
using System.Security.Claims;

namespace EcoScolarWebApi.Services.Contracts
{
    /// <summary>
    /// User service interface.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Get the current user's profile information based on the provided claims principal.
        /// </summary>
        /// <param name="user">Current user's claims principal.</param>
        /// <returns>instance containing the user's profile information if successful.</returns>
        Task<Result<UserReadDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user);
        Task<Result<UserReadDto>> UpdateProfileAsync(ClaimsPrincipal user, UserUpdateDto dto);
        Task<Result<UserPublicReadDto>> GetPublicProfileAsync(string userId);
    }
}