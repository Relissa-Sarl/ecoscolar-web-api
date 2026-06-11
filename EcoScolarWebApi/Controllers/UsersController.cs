using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.DTOs.Stripe;
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
public class UsersController(IUserService userService, UserManager<User> userManager, EcoscolarDbContext context, ReviewMapper reviewMapper, IStripeConnectService stripeConnectService) : ControllerBase
{
	private readonly UserManager<User> _userManager = userManager;
	private readonly IUserService _userService = userService;            // Seller service for handling user-related operations
	private readonly EcoscolarDbContext _context = context;
	private readonly ReviewMapper _reviewMapper = reviewMapper;
	private readonly IStripeConnectService _stripeConnectService = stripeConnectService;

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
        var result = await _userService.AnonymizeProfileAsync(User);

        if (result.IsSuccess)
            return Ok(new { message = "The account has successfully got anonymized" });

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { result.Errors }),
            ErrorType.Unauthorized => Unauthorized(new { result.Errors }),

            _ => BadRequest(new { result.Errors })
        };
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
	
		var alert = new SearchAlert
		{
			UserId = currentUser.Id,
			AdvertSearch = dto.Q?.Trim() ?? string.Empty,
			AdvertType = dto.AdvertType ?? ResolveAdvertType(dto),
			ISBN = dto.Isbn?.Trim(),
			BookCategoryId = dto.BookCategoryId,
			ProductCategoryId = dto.ProductCategoryId,
			SubjectId = dto.SubjectId,
			SchoolGradeId = dto.SchoolGradeId,
			MinPrice = dto.MinPrice,
			MaxPrice = dto.MaxPrice,
			CreatedAt = DateTime.UtcNow
		};
		_context.SearchAlerts.Add(alert);
		await _context.SaveChangesAsync();

		await _context.Entry(alert).Reference(a => a.BookCategory).LoadAsync();
		await _context.Entry(alert).Reference(a => a.ProductCategory).LoadAsync();
		await _context.Entry(alert).Reference(a => a.Subject).LoadAsync();
		await _context.Entry(alert).Reference(a => a.SchoolGrade).LoadAsync();


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
			.Include(a => a.BookCategory)
			.Include(a => a.ProductCategory)
			.Include(a => a.Subject)
			.Include(a => a.SchoolGrade)
			.Where(a => a.UserId == currentUser.Id)
			.OrderByDescending(a => a.ResearchId)
			.ToListAsync();

		var result = alerts.Select(alert =>
		{
			var matchedCount = CountMatches(alert);
			return SearchAlertReadDto.FromEntity(alert, matchedCount);
		});

		return Ok(result);
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

	#region Stripe Connect

	/// <summary>
	/// Creates the Stripe Connect account of the authenticated user if needed,
	/// then generates a Stripe-hosted onboarding link to redirect the seller to.
	/// POST /api/v1/users/me/stripe/onboarding
	/// </summary>
	[HttpPost("me/stripe/onboarding")]
	[ProducesResponseType(typeof(StripeOnboardingResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> CreateStripeOnboardingLink()
	{
		var result = await _stripeConnectService.CreateOnboardingLinkAsync(User, GetFrontendBaseUrl());

		if (result.IsSuccess)
			return Ok(result.Data);

		return result.ErrorType switch
		{
			ErrorType.NotFound => NotFound(new { result.Errors }),
			ErrorType.InternalError => StatusCode(StatusCodes.Status500InternalServerError, new { result.Errors }),

			_ => BadRequest(new { result.Errors })
		};
	}

	/// <summary>
	/// Returns the Stripe Connect status of the authenticated user.
	/// GET /api/v1/users/me/stripe/status
	/// </summary>
	[HttpGet("me/stripe/status")]
	[ProducesResponseType(typeof(StripeStatusDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetStripeStatus()
	{
		var result = await _stripeConnectService.GetStatusAsync(User);

		if (result.IsSuccess)
			return Ok(result.Data);

		return result.ErrorType switch
		{
			ErrorType.NotFound => NotFound(new { result.Errors }),
			ErrorType.InternalError => StatusCode(StatusCodes.Status500InternalServerError, new { result.Errors }),

			_ => BadRequest(new { result.Errors })
		};
	}

	/// <summary>
	/// Resolves the frontend base URL from the Referer header (the SPA runs on a different
	/// origin than the API), falling back to the API's own scheme/host.
	/// Same approach as PaymentsController.Checkout.
	/// </summary>
	private string GetFrontendBaseUrl()
	{
		string baseUrl = $"{Request.Scheme}://{Request.Host}";
		if (Request.Headers.TryGetValue("Referer", out var refererHeader) && !string.IsNullOrEmpty(refererHeader))
		{
			try
			{
				var uri = new Uri(refererHeader.ToString());
				baseUrl = $"{uri.Scheme}://{uri.Authority}";
			}
			catch
			{
				// Fallback in case of malformed Referer
			}
		}
		return baseUrl;
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
			.ThenInclude(a => a!.Seller)
			.Select(uf => uf.Advert)
			.ToListAsync();

		List<long> physicalItemIds = [.. favorites.OfType<PhysicalItem>().Select(item => item.AdvertId)];

		if (physicalItemIds.Any())
		{
			await _context.Pictures
				.Where(Pictures => physicalItemIds.Contains(Pictures.PhysicalItemId))
				.LoadAsync();
		}

		return Ok(favorites
			.Where(a => a != null)
			.Select(a => AdvertReadDto.FromEntity(a!)));
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
		if (!string.IsNullOrWhiteSpace(dto.Isbn) || dto.BookCategoryId.HasValue)
			return CatalogAdvertTypeCodes.Books;

		if (dto.SubjectId.HasValue || dto.SchoolGradeId.HasValue)
			return CatalogAdvertTypeCodes.Service;

		if (dto.ProductCategoryId.HasValue)
			return CatalogAdvertTypeCodes.Product;

		return CatalogAdvertTypeCodes.Books;
	}

		private int CountMatches(SearchAlert alert)
		{
			return alert.AdvertType switch
			{
				CatalogAdvertTypeCodes.Books => CountBookMatches(alert),
				CatalogAdvertTypeCodes.Product => CountProductMatches(alert),
				CatalogAdvertTypeCodes.Service => CountServiceMatches(alert),
				_ => CountAdvertMatches(alert)
			};
		}

		private int CountAdvertMatches(SearchAlert alert)
		{
			var query = _context.Adverts.AsQueryable();

			if (!string.IsNullOrWhiteSpace(alert.AdvertSearch))
			{
				var search = alert.AdvertSearch.Trim();
				query = query.Where(a =>
					EF.Functions.Like(a.Title, $"%{search}%")
					|| EF.Functions.Like(a.Description, $"%{search}%"));
			}

			if (alert.MinPrice.HasValue)
			{
				query = query.Where(a => a.Price >= alert.MinPrice.Value);
			}

			if (alert.MaxPrice.HasValue)
			{
				query = query.Where(a => a.Price <= alert.MaxPrice.Value);
			}

			return query.Count();
		}

		private int CountBookMatches(SearchAlert alert)
		{
			var query = _context.Books.AsQueryable();

			if (!string.IsNullOrWhiteSpace(alert.AdvertSearch))
			{
				var search = alert.AdvertSearch.Trim();
				query = query.Where(b =>
					EF.Functions.Like(b.Title, $"%{search}%")
					|| EF.Functions.Like(b.Description, $"%{search}%"));
			}

			if (!string.IsNullOrWhiteSpace(alert.ISBN))
			{
				var isbn = alert.ISBN.Trim();
				query = query.Where(b => EF.Functions.Like(b.ISBN, $"%{isbn}%"));
			}

			if (alert.BookCategoryId.HasValue)
			{
				query = query.Where(b => b.BookCategoryId == alert.BookCategoryId.Value);
			}

			if (alert.MinPrice.HasValue)
			{
				query = query.Where(b => b.Price >= alert.MinPrice.Value);
			}

			if (alert.MaxPrice.HasValue)
			{
				query = query.Where(b => b.Price <= alert.MaxPrice.Value);
			}

			return query.Count();
		}
		
		private int CountProductMatches(SearchAlert alert)
		{
			var query = _context.Products
				.Where(p => !_context.Books.Any(b => b.AdvertId == p.AdvertId));

			if (!string.IsNullOrWhiteSpace(alert.AdvertSearch))
			{
				var search = alert.AdvertSearch.Trim();
				query = query.Where(p =>
					EF.Functions.Like(p.Title, $"%{search}%")
					|| EF.Functions.Like(p.Description, $"%{search}%"));
			}

			if (alert.ProductCategoryId.HasValue)
			{
				query = query.Where(p => p.ProductCategoryId == alert.ProductCategoryId.Value);
			}

			if (alert.MinPrice.HasValue)
			{
				query = query.Where(p => p.Price >= alert.MinPrice.Value);
			}

			if (alert.MaxPrice.HasValue)
			{
				query = query.Where(p => p.Price <= alert.MaxPrice.Value);
			}

			return query.Count();
		}

		private int CountServiceMatches(SearchAlert alert)
		{
			var query = _context.Services.AsQueryable();

			if (!string.IsNullOrWhiteSpace(alert.AdvertSearch))
			{
				var search = alert.AdvertSearch.Trim();
				query = query.Where(s =>
					EF.Functions.Like(s.Title, $"%{search}%")
					|| EF.Functions.Like(s.Description, $"%{search}%"));
			}

			if (alert.SubjectId.HasValue)
			{
				query = query.Where(s => s.SubjectId == alert.SubjectId.Value);
			}

			if (alert.SchoolGradeId.HasValue)
			{
				query = query.Where(s => s.SchoolGradeId == alert.SchoolGradeId.Value);
			}

			if (alert.MinPrice.HasValue)
			{
				query = query.Where(s => s.Price >= alert.MinPrice.Value);
			}

			if (alert.MaxPrice.HasValue)
			{
				query = query.Where(s => s.Price <= alert.MaxPrice.Value);
			}

			return query.Count();
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
