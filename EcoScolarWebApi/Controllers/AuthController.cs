using EcoscolarWebApi.Services;
using EcoscolarWebApi.Utils;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    /// <summary>
    /// Defines API endpoints for user authentication operations such as registration and login.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;     // User service instance reference

        /// <summary>
        /// Initializes a new instance of the AuthController class using the specified user service.
        /// </summary>
        /// <param name="userService">The user service to be used for authentication and user-related operations. Cannot be null.</param>
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Registers a new user account using the provided registration details.
        /// </summary>
        /// <param name="request">The registration information for the new user. Must include all required fields as defined by the
        /// registration process.</param>
        /// <returns>A 201 Created response if registration is successful; otherwise, a 400 Bad Request response containing
        /// validation errors.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Get the result of the registration attempt from the user service
            var result = await _userService.RegisterUserAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { result.Errors });

            return Created(string.Empty, result.Data);
        }

        /// <summary>
        /// Authenticates a user with the provided credentials and returns an appropriate HTTP response.
        /// </summary>
        /// <param name="dto">The login credentials and related information required to authenticate the user. Cannot be null.</param>
        /// <returns>An HTTP 200 OK response if authentication succeeds; otherwise, an HTTP 401 Unauthorized response with error
        /// details.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            // Get the result of the login attempt from the user service
            var result = await _userService.LoginUserAsync(dto);

            if (result.IsSuccess)
                return Ok();

            // Dispatch response based on the specific error type returned by the service
            return result.ErrorType switch
            {
                // 401 Unauthorized for bad credentials
                ErrorType.Unauthorized => Unauthorized(new { result.Errors }),

                // 400 Bad Request as a safety fallback
                _ => BadRequest(new { result.Errors })
            };
        }
    }
}