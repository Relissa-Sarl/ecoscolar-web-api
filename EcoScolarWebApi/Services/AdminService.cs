using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<User> _userManager;            // Seller manager
        private readonly SignInManager<User> _signInManager;        // Sign-in manager
        private readonly EcoscolarDbContext _context;               // Database context
        private readonly UserMapper _userMapper;                    // User mapper for converting between entities and DTOs

        public AdminService(UserManager<User> userManager, EcoscolarDbContext dbContext, SignInManager<User> signInManager, UserMapper userMapper)
        {
            _userManager = userManager;
            _context = dbContext;
            _signInManager = signInManager;
            _userMapper = userMapper;
        }

        /// <summary>
        /// Get all users if the connected user is admin
        /// </summary>
        /// <param name="user">Connected user principal</param>
        /// <returns>A Result object with a UserResponse DTO value; otherwise, a failure result indicating the reason.</returns>
        public async Task<Result<List<UserResponse>>> GetAllUsers(ClaimsPrincipal user)
        {
            if (!user.IsInRole("Admin"))
                return Result<List<UserResponse>>.Failure("Unauthorized access.", ErrorType.Unauthorized);
            var users = await _userManager.Users.
                Include(u => u.Languages)
                .ToListAsync();
            var userDtos = new List<UserResponse>();

            foreach (var item in users)
            {
                userDtos.Add(_userMapper.ToResponse(item) with { Roles = (await _userManager.GetRolesAsync(item)).ToArray() });
            }
            return Result<List<UserResponse>>.Success(userDtos);
        }

        /// <summary>
        /// Ban or unban a users if the connected user is admin
        /// </summary>
        /// <param name="user">Connected user principal</param>
        /// <param name="userId">ID of the user to ban</param>
        /// <returns>A Result object with a UserResponse DTO value; otherwise, a failure result indicating the reason.</returns>
        public async Task<Result<UserResponse>> BanUserToggle(ClaimsPrincipal user, string userId)
        {
            var currentUser = await _userManager.Users
                .Include(u => u.Languages)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
                return Result<UserResponse>.Failure("User not found.", ErrorType.NotFound);

            if (!user.IsInRole("Admin") || _userManager.IsInRoleAsync(currentUser, "Admin").Result)
                return Result<UserResponse>.Failure("Unauthorized access.", ErrorType.Unauthorized);

            currentUser.IsBanned = !currentUser.IsBanned;
            if (currentUser.IsBanned)
            {
                //await _userManager.SetLockoutEnabledAsync(currentUser, true);
                await _userManager.SetLockoutEndDateAsync(currentUser, DateTime.Today.AddYears(999));
            }
            else
            {
                //await _userManager.SetLockoutEnabledAsync(currentUser, false);
                await _userManager.SetLockoutEndDateAsync(currentUser, null);
            }

            var updateResult = await _userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description);
                return Result<UserResponse>.Failure(errors);
            }

            var roles = await _userManager.GetRolesAsync(currentUser);

            return Result<UserResponse>.Success(_userMapper.ToResponse(currentUser) with { Roles = roles.ToArray() });
        }
    }
}
