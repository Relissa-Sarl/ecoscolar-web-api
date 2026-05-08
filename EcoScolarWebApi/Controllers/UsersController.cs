using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;            // User manager provided by ASP.NET Core Identity for user management operations

        /// <summary>
        /// UsersController constructor
        /// </summary>
        /// <param name="userManager">The user manager for handling user management operations</param>
        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Register a new user with the provided information in the UserDto object. 
        /// This endpoint is accessible without authentication and uses the UserManager 
        /// to create a new user and hash the password automatically.
        /// 
        /// Url: POST /api/v1/users/register
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCustom([FromBody] UserDto request)
        {
            // Create a new user object with the provided information
            User newUser = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            // Ask identity to save the user and hash the password automatically
            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (result.Succeeded)
                return Ok(new { message = "User created successfully!" });

            return BadRequest(result.Errors);
        }

        /// <summary>
        /// Get the profile information of the currently authenticated user. 
        /// This endpoint requires authentication and retrieves the user's information using 
        /// the UserManager based on the current user context.
        /// 
        /// Url: GET /api/v1/users/me
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Get the current user from the UserManager using the current user context
            var currentUser = await _userManager.GetUserAsync(User);

            // If the user is not found, return a 404 Not Found response
            if (currentUser == null)
                return NotFound(new { message = "Utilisateur introuvable." });

            // Create a DTO to return only the relevant user information to the client
            var userProfileDto = new
            {
                Id = currentUser.Id,
                UserName = currentUser.UserName,
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                Email = currentUser.Email,
            };

            return Ok(userProfileDto);
        }
    }
}
