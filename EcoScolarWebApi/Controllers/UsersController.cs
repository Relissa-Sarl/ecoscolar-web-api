using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

/// <summary>
/// UsersController constructor
/// </summary>
/// <param name="userService">The user service for handling user-related operations</param>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController(IUserService userService, UserManager<User> userManager, EcoscolarDbContext context, ReviewMapper reviewMapper) : ControllerBase
{
	private readonly UserManager<User> _userManager = userManager;
	private readonly IUserService _userService = userService;            // Seller service for handling user-related operations
	private readonly EcoscolarDbContext _context = context;
	private readonly ReviewMapper _reviewMapper = reviewMapper;

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
		// Pass the HTTP session's Seller directly to the service
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

	/// <summary>
	/// Creates a search alert for the authenticated user.
	/// POST /api/v1/users/me/search-alerts
	/// </summary>
	[HttpPost("me/search-alerts")]
	public async Task<IActionResult> CreateSearchAlert([FromBody] CreateSearchAlertDto dto)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser == null)
			return NotFound(new { message = "Seller not found." });

		if (!dto.HasAnyCriterion())
			return BadRequest(new { message = "At least one search criterion is required." });

		long? subjectId = null;
		if (!string.IsNullOrWhiteSpace(dto.Subjects))
		{
			var subject = await _context.Subjects
				.FirstOrDefaultAsync(s => s.Name == dto.Subjects.Trim());
			subjectId = subject?.SubjectId;
		}
		long? bookCategoryId = null;
		if (!string.IsNullOrWhiteSpace(dto.Category))
		{
			var category = await _context.BookCategories
				.FirstOrDefaultAsync(c => c.Name == dto.Category.Trim());
			bookCategoryId = category?.BookCategoryId;
		}
	
		var alert = new SearchAlert
		{
			UserId = currentUser.Id,
			AdvertSearch = dto.Q?.Trim() ?? string.Empty,
			AdvertType = ResolveAdvertType(dto),
			ISBN = dto.Isbn?.Trim(),
			MaxPrice = dto.MaxPrice,
			SubjectId = subjectId,
			BookCategoryId = bookCategoryId
		};
		_context.SearchAlerts.Add(alert);
		await _context.SaveChangesAsync();

		return StatusCode(StatusCodes.Status201Created, SearchAlertReadDto.FromEntity(alert));
	}
	
	/// <summary>
	/// Retrieves the list of search alerts for the authenticated user.
	/// GET /api/v1/users/me/search-alerts
	/// </summary>
	[HttpGet("me/search-alerts")]
	public async Task<IActionResult> GetMySearchAlerts()
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser == null)
			return NotFound(new { message = "Seller not found." });
		var alerts = await _context.SearchAlerts
			.Where(a => a.UserId == currentUser.Id)
			.OrderByDescending(a => a.ResearchId)
			.ToListAsync();
		return Ok(alerts.Select(SearchAlertReadDto.FromEntity));
	}

	/// <summary>
	/// Deletes a search alert owned by the authenticated user.
	/// DELETE /api/v1/users/me/search-alerts/{id}
	/// </summary>
	[HttpDelete("me/search-alerts/{id:int}")]
	public async Task<IActionResult> DeleteSearchAlert(int id)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser == null)
			return NotFound(new { message = "Seller not found." });

		var alert = await _context.SearchAlerts
			.FirstOrDefaultAsync(a => a.ResearchId == id && a.UserId == currentUser.Id);

		if (alert == null)
			return NotFound(new { message = "Search alert not found." });

		_context.SearchAlerts.Remove(alert);
		await _context.SaveChangesAsync();

		return NoContent();
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
			return NotFound(new { message = "Seller not found." });

		var adverts = await _context.Adverts
			.Where(a => a.SellerId == currentUser.Id)
			.Include(a => a.Seller)
			.ToListAsync();
		List<long> physicalItemIds = adverts.OfType<PhysicalItem>()
			.Select(item => item.AdvertId)
			.ToList();
		if (physicalItemIds.Any())
		{
			await _context.Pictures
				.Where(Pictures => physicalItemIds.Contains(Pictures.PhysicalItemId))
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
			return NotFound(new { message = "Seller not found." });

		var favorites = await _context.UserFavorites
			.Where(uf => uf.UserId == currentUser.Id)
			.Include(uf => uf.Advert)
			.ThenInclude(a => a.Seller)
			.Select(uf => uf.Advert)
			.ToListAsync();

		List<long> physicalItemIds = [.. favorites.OfType<PhysicalItem>().Select(item => item.AdvertId)];

		if (physicalItemIds.Any())
		{
			await _context.Pictures
				.Where(Pictures => physicalItemIds.Contains(Pictures.PhysicalItemId))
				.LoadAsync();
		}

		return Ok(favorites.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Toggles a specific PhysicalItem in the authenticated user's favorites list. Add to favorites if not present, otherwise remove it.
	/// 
	/// Url: PATCH /api/v1/users/me/favorites/{advertId}
	/// </summary>
	/// <param name="advertId">The ID of the PhysicalItem to toggle in favorites</param>
	/// <returns>A status indicating whether the PhysicalItem is currently a favorite or not</returns>
	[HttpPatch("me/favorites/{advertId}")]
	public async Task<IActionResult> ToggleFavorite(long advertId)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser == null)
			return NotFound(new { message = "Seller not found." });

		var Adverts = await _context.Adverts.FindAsync(advertId);
		if (Adverts == null)
			return NotFound(new { message = "PhysicalItem not found." });

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

	private static string ResolveAdvertType(CreateSearchAlertDto dto)
	{
		if (!string.IsNullOrWhiteSpace(dto.Isbn))
			return CatalogAdvertTypeCodes.Books;

		if (!string.IsNullOrWhiteSpace(dto.Subjects) || !string.IsNullOrWhiteSpace(dto.Grade))
			return CatalogAdvertTypeCodes.Service;

		if (!string.IsNullOrWhiteSpace(dto.Category))
			return CatalogAdvertTypeCodes.Product;

		return CatalogAdvertTypeCodes.Books;
	}
	#endregion

	#region Reviews
	[HttpGet("{userId}/reviews")]
	public async Task<ActionResult<IEnumerable<ReviewResponseDTO>>> GetUserReviews(string userId)
	{
		var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
		if (!userExists)
			return NotFound();

		var reviews = await _reviewMapper.ProjectToReviewResponseDTOs(
			_context.Reviews.Where(r => r.ReviewedId == userId))
			.ToListAsync();

		return Ok(reviews);
	}
	#endregion
}
