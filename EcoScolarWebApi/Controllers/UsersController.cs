using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // POST /register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCustom([FromBody] UserDto request)
        {
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

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound(new { message = "Utilisateur introuvable." });
            }

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
