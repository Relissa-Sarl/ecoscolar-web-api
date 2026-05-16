using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoscolarWebApi.Services.Impl
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
        /// Registers a new user with the provided registration details asynchronously.
        /// </summary>
        /// <param name="dto">An object containing the user's registration information, including email, nickname, password, personal
        /// details, spoken languages, and postal code. Cannot be null.</param>
        /// <returns>The task result contains a Result object with the registered user's data if successful; 
        /// otherwise, a failure result with error messages.</returns>
        public async Task<Result<UserReadDto>> RegisterUserAsync(RegisterRequestDto dto)
        {
            // Get the location for the user's postal code
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.PostalCode == dto.PostalCode);

            if (location == null)
                return Result<UserReadDto>.Failure("Invalid postal code.");

            // Create a new user with his information
            User newUser = new User
            {
                UserName = dto.Nickname,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                BirthdayDate = dto.BirthdayDate,
                Languages = dto.SpokenLanguages.Select(lang => new UserLanguage
                {
                    Label = lang.Language,
                    LanguageLevel = lang.Level
                }).ToList(),
                LocationId = location.LocationId
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);

            // Return the result of the request
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Result<UserReadDto>.Failure(errors);
            }

            var userDto = UserReadDto.fromEntity(newUser);

            return Result<UserReadDto>.Success(userDto);
        }

        /// <summary>
        /// Attempts to authenticate a user using the provided login credentials.
        /// </summary>
        /// <param name="dto">An object containing the user's email and password to be used for authentication. Cannot be null.</param>
        /// <returns>A result indicating whether the login was successful. Returns a failure result with an unauthorized error
        /// type if the credentials are invalid.</returns>
        public async Task<Result> LoginUserAsync(LoginRequestDto dto)
        {
            // Get the user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
                return Result.Failure("Invalid credentials.", ErrorType.Unauthorized);

            // Sign in the user with the provided password, creating a persistent token
            var result = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
                return Result.Failure("Invalid credentials.", ErrorType.Unauthorized);

            return Result.Success();
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
    }
}
