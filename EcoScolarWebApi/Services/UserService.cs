using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Services
{
    /// <summary>
    /// Implements user-related business logic.
    /// </summary>  
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;            // User manager
        private readonly SignInManager<User> _signInManager;        // Sign-in manager
        private readonly EcoscolarDbContext _context;               // Database context

        /// <summary>
        /// Initialize the service with required dependencies.
        /// </summary>
        /// <param name="userManager">User manager.</param>
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
                return Result<UserReadDto>.Failure("User not found or session expired.", ErrorType.NotFound);

            // Build the dto for the response
            var userDto = UserReadDto.fromEntity(currentUser);

            return Result<UserReadDto>.Success(userDto);
        }

        public async Task<Result<UserReadDto>> UpdateProfileAsync(ClaimsPrincipal user, UserUpdateDto dto)
        {
            var currentUserId = _userManager.GetUserId(user);
            var currentUser = await _userManager.Users
                .Include(u => u.Languages)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
                return Result<UserReadDto>.Failure("User not found", ErrorType.NotFound);

            var location = await _context.Locations.FirstOrDefaultAsync(l => l.PostalCode == dto.PostalCode);
            if (location == null)
                return Result<UserReadDto>.Failure("Invalid postal code");

            currentUser.Nickname = dto.Nickname;
            currentUser.FirstName = dto.FirstName;
            currentUser.LastName = dto.LastName;
            currentUser.BirthdayDate = dto.BirthdayDate;
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

            return Result<UserReadDto>.Success(UserReadDto.fromEntity(currentUser));
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
                    "User not found or profile is not public yet.",
                    ErrorType.NotFound
                );

            // Return the safe public DTO
            return Result<UserPublicReadDto>.Success(UserPublicReadDto.fromEntity(user));
        }
    }
}
