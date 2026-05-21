using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
	private readonly UserManager<User> _userManager;
	private readonly IUserService _userService;            // User service for handling user-related operations
	private readonly EcoscolarDbContext _context;

	/// <summary>
	/// UsersController constructor
	/// </summary>
	/// <param name="userService">The user service for handling user-related operations</param>
	public UsersController(IUserService userService, UserManager<User> userManager, EcoscolarDbContext context)
	{
		_userService = userService;
		_userManager = userManager;
		_context = context;
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
			ErrorType.Unauthorized => Unauthorized(new { result.Errors }),

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
		List<long> physicalItemIds = adverts.OfType<PhysicalItem>()
			.Select(item => item.AdvertId)
			.ToList();
		if (physicalItemIds.Any())
		{
			await _context.Pictures
				.Where(Pictures => physicalItemIds.Contains(Pictures.AdvertId))
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
			.Include(uf => uf.Adverts)
			.ThenInclude(a => a.User)
			.Select(uf => uf.Adverts)
			.ToListAsync();

		List<long> physicalItemIds = [.. favorites.OfType<PhysicalItem>().Select(item => item.AdvertId)];

		if (physicalItemIds.Any())
		{
			await _context.Pictures
				.Where(Pictures => physicalItemIds.Contains(Pictures.AdvertId))
				.LoadAsync();
		}

		return Ok(favorites.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Toggles a specific Adverts in the authenticated user's favorites list. Add to favorites if not present, otherwise remove it.
	/// 
	/// Url: PATCH /api/v1/users/me/favorites/{advertId}
	/// </summary>
	/// <param name="advertId">The ID of the Adverts to toggle in favorites</param>
	/// <returns>A status indicating whether the Adverts is currently a favorite or not</returns>
	[HttpPatch("me/favorites/{advertId}")]
	public async Task<IActionResult> ToggleFavorite(long advertId)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser == null)
			return NotFound(new { message = "User not found." });

		var Adverts = await _context.Adverts.FindAsync(advertId);
		if (Adverts == null)
			return NotFound(new { message = "Adverts not found." });

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
	#endregion
}
