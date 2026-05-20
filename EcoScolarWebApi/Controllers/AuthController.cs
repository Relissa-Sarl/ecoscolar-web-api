using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoScolarWebApi.Controllers
{
    /// <summary>
    /// Defines API endpoints for user authentication operations such as registration and login.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;             // User service instance reference
        private readonly SignInManager<User> _signInManager;    // Sign in manager provided by Identity

        /// <summary>
        /// Initializes a new instance of the AuthController class using the specified user service.
        /// </summary>
        /// <param name="userService">The user service to be used for authentication and user-related operations. Cannot be null.</param>
        /// <param name="signInManager">The sign in manager provided by Identity to manage the current user connection</param>
        public AuthController(IUserService userService, SignInManager<User> signInManager)
        {
            _userService = userService;
            _signInManager = signInManager;
        
        }

        /// <summary>
        /// Signs out the currently authenticated user.
        /// </summary>
        /// <param name="signInManager">The SignInManager instance used to manage user sign-in and sign-out operations. Cannot be null.</param>
        /// <returns>An IActionResult that indicates the result of the sign-out operation.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok();
        }
    }
}