using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Mappers;
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
    private readonly UserMapper _userMapper;                    // User mapper for converting between entities and DTOs

    /// <summary>
    /// Initialize the service with required dependencies.
    /// </summary>
    /// <param name="userManager">Seller manager.</param>
    /// <param name="dbContext">Database context.</param>
    /// <param name="signInManager">Sign-in manager.</param>
    /// <param name="userMapper">User mapper.</param>
    public UserService(UserManager<User> userManager, EcoscolarDbContext dbContext, SignInManager<User> signInManager, UserMapper userMapper)
    {
        _userManager = userManager;
        _context = dbContext;
        _signInManager = signInManager;
        _userMapper = userMapper;
    }

    /// <summary>
    /// Retrieves the profile information for the currently authenticated user.
    /// </summary>
    /// <param name="user">The claims principal representing the current authenticated user. Cannot be null.</param>
    /// <returns>The task result contains a Result object with the user's profile data if found; otherwise, 
    /// a failure result indicating the reason.</returns>
    public async Task<Result<UserResponse>> GetCurrentUserProfileAsync(ClaimsPrincipal user)
    {
        // Get the current user ID
        var userId = _userManager.GetUserId(user);

        if (string.IsNullOrEmpty(userId))
            return Result<UserResponse>.Failure("Invalid session.", ErrorType.Unauthorized);

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
            return Result<UserResponse>.Failure("Seller not found or session expired.", ErrorType.NotFound);

        // Build the dto for the response
        var userDto = _userMapper.ToResponse(currentUser);

        var roles = await _userManager.GetRolesAsync(currentUser);

        return Result<UserResponse>.Success(userDto with { Roles = roles.ToArray() });
    }

    public async Task<Result<UserResponse>> UpdateProfileAsync(ClaimsPrincipal user, UserUpdateDto dto)
    {
        var currentUserId = _userManager.GetUserId(user);
        var currentUser = await _userManager.Users
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (currentUser == null)
            return Result<UserResponse>.Failure("User not found", ErrorType.NotFound);

        var location = await _context.Locations.FirstOrDefaultAsync(l => l.PostalCode == dto.PostalCode);
        if (location == null)
            return Result<UserResponse>.Failure("InvalidPostalCode");

        currentUser.Nickname = dto.Nickname;
        currentUser.FirstName = dto.FirstName;
        currentUser.LastName = dto.LastName;
        currentUser.DateOfBirth = dto.BirthdayDate;
        currentUser.LocationId = location.LocationId;

        currentUser.Languages.Clear();
        currentUser.Languages = dto.Languages.Select(lang => new UserLanguage
        {
            Label = lang.Label,
            LanguageLevel = lang.LanguageLevel
        }).ToList();

        currentUser.IsOnboarded = true;

        var updateResult = await _userManager.UpdateAsync(currentUser);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description);
            return Result<UserResponse>.Failure(errors);
        }
        var resultRole = await _userManager.AddToRoleAsync(currentUser, "User");

        var roles = await _userManager.GetRolesAsync(currentUser);

        return Result<UserResponse>.Success(_userMapper.ToResponse(currentUser) with { Roles = roles.ToArray() });
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
            return Result<bool>.Failure("SESSION_EXPIRED", ErrorType.NotFound);

        // Hash personal information to anonymize the user while keeping the nickname unique
        var salt = Guid.NewGuid().ToString("N");

        currentUser.FirstName = Hasher.HashString($"{salt}:{currentUser.FirstName ?? string.Empty}");
        currentUser.LastName = Hasher.HashString($"{salt}:{currentUser.LastName ?? string.Empty}");
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

        var setEmailResult = await _userManager.SetEmailAsync(currentUser, anonymousEmail);
        if (!setEmailResult.Succeeded)
            return Result<bool>.Failure(setEmailResult.Errors.Select(e => e.Description));

        var setUserNameResult = await _userManager.SetUserNameAsync(currentUser, anonymousEmail);
        if (!setUserNameResult.Succeeded)
            return Result<bool>.Failure(setUserNameResult.Errors.Select(e => e.Description));

        currentUser.NormalizedEmail = _userManager.NormalizeEmail(anonymousEmail);
        currentUser.NormalizedUserName = _userManager.NormalizeName(anonymousEmail);
        currentUser.PasswordHash = Guid.NewGuid().ToString();
        currentUser.PhoneNumber = null;

        // Delete user favorites and mark as not onboarded to hide the profile from public listings
        _context.UserFavorites.RemoveRange(currentUser.Favorites);
        currentUser.IsOnboarded = false;

        // Save the data
        var updateResult = await _userManager.UpdateAsync(currentUser);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description);
            return Result<bool>.Failure(errors);
        }

        await _context.SaveChangesAsync();

        // Sign out the user to invalidate their session
        await _signInManager.SignOutAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ReportUserAsync(ClaimsPrincipal userPrincipal, string flaggedUserId, string reason)
    {
        var reporterId = _userManager.GetUserId(userPrincipal);

        if (string.IsNullOrEmpty(reporterId))
            return Result<bool>.Failure("SESSION_INVALID", ErrorType.Unauthorized);

        if (reporterId == flaggedUserId)
            return Result<bool>.Failure("You cannot report yourself.");

        var flaggedUserExists = await _userManager.Users.AnyAsync(u => u.Id == flaggedUserId);
        if (!flaggedUserExists)
            return Result<bool>.Failure("User to report not found.", ErrorType.NotFound);

        //var alreadyReported = await _context.Flags
        //    .AnyAsync(f => f.ReporterId == reporterId && f.FlaggedId == flaggedUserId);

        //if (alreadyReported)
        //    return Result<bool>.Failure("You have already reported this user.");

        var flag = new Flag
        {
            ReporterId = reporterId,
            FlaggedId = flaggedUserId,
            Reason = reason,
            Date = DateTime.UtcNow
        };

        _context.Flags.Add(flag);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
