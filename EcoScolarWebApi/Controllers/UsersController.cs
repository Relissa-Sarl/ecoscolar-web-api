using EcoscolarWebApi.Services.Contracts;
using EcoscolarWebApi.Utils;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;            // User service for handling user-related operations

        /// <summary>
        /// UsersController constructor
        /// </summary>
        /// <param name="userService">The user service for handling user-related operations</param>
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        #region Current user

        /// <summary>
        /// Get the profile information of the currently authenticated user. 
        /// This endpoint requires authentication and retrieves the user's information using 
        /// the UserManager based on the current user context.
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Pass the HTTP session's User directly to the service
            var result = await _userService.GetCurrentUserProfileAsync(User);

            // If successful, return 200 OK along with the user's data
            if (result.IsSuccess)
                return Ok(result.Data);

            // Dispatch the response depending on the error code
            return result.ErrorType switch
            {
                // 401 Unauthorized if the user isn't connected
                ErrorType.Unauthorized => Unauthorized (new { result.Errors } ),

                // 404 Not Found if the user was deleted
                ErrorType.NotFound => NotFound(new { result.Errors }),

                // 400 Bad Request fallback
                _ => BadRequest(new { result.Errors })
            };
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateFullProfile([FromBody] UserUpdateDto dto)
        {
            // This single method handles both initial onboarding and later profile updates
            var result = await _userService.UpdateProfileAsync(User, dto);

            if (result.IsSuccess)
                return Ok(result.Data);

            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { result.Errors }),

                _ => BadRequest(new { result.Errors })
            };
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            return null;
        }

        #endregion ===

        #region Public profiles

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserProfile(string id)
        {
            var result = await _userService.GetPublicProfileAsync(id);

            if (result.IsSuccess)
                return Ok(result.Data);

            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { result.Errors }),

                _ => BadRequest(new { result.Errors })
            };
        }

        #endregion
    }
}
