using EcoscolarWebApi.Utils;
using EcoscolarWebApi.Utils.DTOs;
using System.Security.Claims;

namespace EcoscolarWebApi.Services
{
    /// <summary>
    /// User service interface.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Register a new user with the provided information.
        /// </summary>
        /// <param name="dto">Registration data.</param>
        /// <returns>Tuple indicating success and any errors.</returns>
        Task<Result<UserReadDto>> RegisterUserAsync(RegisterRequestDto dto);

        /// <summary>
        /// Authenticate a user by email and password.
        /// </summary>
        /// <param name="dto">Login data.</param>
        /// <returns>Tuple indicating success and any errors.</returns>
        Task<Result> LoginUserAsync(LoginRequestDto dto);

        /// <summary>
        /// Get the current user's profile information based on the provided claims principal.
        /// </summary>
        /// <param name="user">Current user's claims principal.</param>
        /// <returns>instance containing the user's profile information if successful.</returns>
        Task<Result<UserReadDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user);
    }
}