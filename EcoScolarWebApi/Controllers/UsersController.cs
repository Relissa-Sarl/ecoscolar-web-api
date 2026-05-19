using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using EcoscolarWebApi.Utils.DTOs.Advert;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;            // User manager provided by ASP.NET Core Identity for user management operations
        private readonly EcoscolarDbContext _context;                // Database context for accessing the database

        /// <summary>
        /// UsersController constructor
        /// </summary>
        /// <param name="userManager">The user manager for handling user management operations</param>
        public UsersController(UserManager<User> userManager, EcoscolarDbContext context)
        {
            _userManager = userManager;
            _context = context;
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
                return NotFound(new { message = "User not found." });

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

        [HttpGet("me/adverts")]
        public async Task<IActionResult> GetMyAdverts()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return NotFound(new { message = "User not found." });

            var adverts = await _context.Adverts
                .Where(a => a.UserId == currentUser.Id)
                .Include(a => a.User)
                .ToListAsync();
            List<long> physicalItemIds = adverts.OfType<PhysicalItems>()
                .Select(item => item.AdvertId)
                .ToList();
            if (physicalItemIds.Any())
            {
                await _context.Pictures
                    .Where(picture => physicalItemIds.Contains(picture.AdvertId))
                    .LoadAsync();
            }
            return Ok(adverts.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Retrieves the list of favorite adverts for the currently authenticated user.
        /// 
        /// Url: GET /api/v1/users/me/favorites
        /// </summary>
        /// <returns>List of favorite adverts data transfer objects</returns>
        [HttpGet("me/favorites")]
        [Authorize]
        public async Task<IActionResult> GetMyFavorites()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return NotFound(new { message = "User not found." });

            var favorites = await _context.UserFavorites
                .Where(uf => uf.UserId == currentUser.Id)
                .Include(uf => uf.Advert)
                .ThenInclude(a => a.User)
                .Select(uf => uf.Advert)
                .ToListAsync();

            List<long> physicalItemIds = [.. favorites.OfType<PhysicalItems>().Select(item => item.AdvertId)];

            if (physicalItemIds.Any())
            {
                await _context.Pictures
                    .Where(picture => physicalItemIds.Contains(picture.AdvertId))
                    .LoadAsync();
            }

            return Ok(favorites.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Toggles a specific advert in the authenticated user's favorites list. Add to favorites if not present, otherwise remove it.
        /// 
        /// Url: PATCH /api/v1/users/me/favorites/{advertId}
        /// </summary>
        /// <param name="advertId">The ID of the advert to toggle in favorites</param>
        /// <returns>A status indicating whether the advert is currently a favorite or not</returns>
        [HttpPatch("me/favorites/{advertId}")]
        public async Task<IActionResult> ToggleFavorite(long advertId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return NotFound(new { message = "User not found." });

            var advert = await _context.Adverts.FindAsync(advertId);
            if (advert == null)
                return NotFound(new { message = "Advert not found." });

            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == currentUser.Id && uf.AdvertId == advertId);

            bool isFavorite;

            if (favorite != null)
            {
                _context.UserFavorites.Remove(favorite);
                isFavorite = false;
            }
            else
            {
                var newFavorite = new UserFavorite
                {
                    UserId = currentUser.Id,
                    AdvertId = advertId
                };
                _context.UserFavorites.Add(newFavorite);
                isFavorite = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { AdvertId = advertId.ToString(), IsFavorite = isFavorite });
        }
    }
}
