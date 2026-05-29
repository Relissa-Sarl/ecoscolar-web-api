using EcoscolarWebApi.Commun;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Services;

/// <summary>
/// Implements user-related business logic.
/// </summary>  
public class UserService : IUserService
{
	private readonly UserManager<User> _userManager;            // Seller manager
	private readonly SignInManager<User> _signInManager;        // Sign-in manager
	private readonly EcoscolarDbContext _context;               // Database context

	/// <summary>
	/// Initialize the service with required dependencies.
	/// </summary>
	/// <param name="userManager">Seller manager.</param>
	/// <param name="dbContext">Database context.</param>
	/// <param name="signInManager">Sign-in manager.</param>
	public UserService(UserManager<User> userManager, EcoscolarDbContext dbContext, SignInManager<User> signInManager)
	{
		_userManager = userManager;
		_context = dbContext;
		_signInManager = signInManager;
	}

	/// <summary>
	/// Retrieves the profile information for the currently authenticated user.
	/// </summary>
	/// <param name="user">The claims principal representing the current authenticated user. Cannot be null.</param>
	/// <returns>The task result contains a Result object with the user's profile data if found; otherwise, 
	/// a failure result indicating the reason.</returns>
	public async Task<Result<UserReadDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user)
	{
		// Get the current user ID
		var userId = _userManager.GetUserId(user);

		if (string.IsNullOrEmpty(userId))
			return Result<UserReadDto>.Failure("Invalid session.", ErrorType.Unauthorized);

		// Get the relations for the user
		var currentUser = await _userManager.Users
			// Languages relations
			.Include(u => u.Languages)
			.ThenInclude(ul => ul.Language)
			// Location relation
			.Include(u => u.Location)
			// Get the user by its ID
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (currentUser == null)
			return Result<UserReadDto>.Failure("Seller not found or session expired.", ErrorType.NotFound);

		// Build the dto for the response
		var userDto = UserReadDto.FromEntity(currentUser);

		return Result<UserReadDto>.Success(userDto);
	}

	public async Task<Result<UserReadDto>> UpdateProfileAsync(ClaimsPrincipal user, UserUpdateDto dto)
	{
		var currentUserId = _userManager.GetUserId(user);
		var currentUser = await _userManager.Users
			.Include(u => u.Languages)
			.FirstOrDefaultAsync(u => u.Id == currentUserId);

		if (currentUser == null)
			return Result<UserReadDto>.Failure("Seller not found", ErrorType.NotFound);

		var location = await _context.Locations.FirstOrDefaultAsync(l => l.PostalCode == dto.PostalCode);
		if (location == null)
			return Result<UserReadDto>.Failure("Invalid postal code");

		currentUser.Nickname = dto.Nickname;
		currentUser.FirstName = dto.FirstName;
		currentUser.LastName = dto.LastName;
		currentUser.DateOfBirth = dto.BirthdayDate;
		currentUser.LocationId = location.LocationId;

		currentUser.Languages.Clear();
		currentUser.Languages = dto.SpokenLanguages.Select(lang => new UserLanguage
		{
			Label = lang.Language,
			LanguageLevel = lang.Level
		}).ToList();

		currentUser.IsOnboarded = true;

		var updateResult = await _userManager.UpdateAsync(currentUser);
		if (!updateResult.Succeeded)
		{
			var errors = updateResult.Errors.Select(e => e.Description);
			return Result<UserReadDto>.Failure(errors);
		}

		return Result<UserReadDto>.Success(UserReadDto.FromEntity(currentUser));
	}

	public async Task<Result<UserPublicReadDto>> GetPublicProfileAsync(string userId)
	{
		// Fetch the user by their ID, including their languages
		var user = await _userManager.Users
			.Include(u => u.Languages)
			.FirstOrDefaultAsync(u => u.Id == userId);

		// If the user doesn't exist, OR if they haven't finished onboarding yet
		if (user == null || !user.IsOnboarded)
			return Result<UserPublicReadDto>.Failure(
				"Seller not found or profile is not public yet.",
				ErrorType.NotFound
			);

		// Return the safe public DTO
		return Result<UserPublicReadDto>.Success(UserPublicReadDto.FromEntity(user));
	}

    /// <summary>
    ///	Anonymize the currently connected user profile when deleting his account
    /// </summary>
    /// <param name="userPrincipal">Connected user principal</param>
    /// <returns>A Result object with a boolean value; otherwise, a failure result indicating the reason.</returns>
    public async Task<Result<bool>> AnonymizeProfileAsync(ClaimsPrincipal userPrincipal)
    {
        var userId = _userManager.GetUserId(userPrincipal);

        if (string.IsNullOrEmpty(userId))
            return Result<bool>.Failure("SESSION_INVALID", ErrorType.Unauthorized);

        var currentUser = await _userManager.Users
            .Include(u => u.Favorites)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser == null)
            return Result<bool>.Failure("SEESION_EXPIRED", ErrorType.NotFound);

        // Hash personal information to anonymize the user while keeping the nickname unique
        string salt = Guid.NewGuid().ToString();

        currentUser.FirstName = Hasher.HashString(currentUser.FirstName ?? salt);
        currentUser.LastName = Hasher.HashString(currentUser.LastName ?? salt);
        currentUser.Nickname = $"DeletedUser_{salt[..8]}"; // Ensure nickname remains unique

        if (!string.IsNullOrEmpty(currentUser.DateOfBirth) && currentUser.DateOfBirth.Length >= 4)
        {
            string year = currentUser.DateOfBirth[..4];
            currentUser.DateOfBirth = $"{year}-01-01";
        }
        else
            currentUser.DateOfBirth = "2000-01-01";

        // Hash the native Identity User properties
        var anonymousEmail = $"{salt[..8]}@deleted.ecoscolar.com";
        await _userManager.SetEmailAsync(currentUser, anonymousEmail);
        await _userManager.SetUserNameAsync(currentUser, anonymousEmail);

        currentUser.NormalizedEmail = anonymousEmail.ToUpper();
        currentUser.NormalizedUserName = anonymousEmail.ToUpper();
        currentUser.PasswordHash = Guid.NewGuid().ToString();
        currentUser.PhoneNumber = null;

        // Delete user favorites and mark as not onboarded to hide the profile from public listings
        currentUser.Favorites.Clear();
        currentUser.IsOnboarded = false;

        // Save the data
        var updateResult = await _userManager.UpdateAsync(currentUser);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description);
            return Result<bool>.Failure(errors);
        }

        // Sign out the user to invalidate their session
        await _signInManager.SignOutAsync();

        return Result<bool>.Success(true);
    }
}
