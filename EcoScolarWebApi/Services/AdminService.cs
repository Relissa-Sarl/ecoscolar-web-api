using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Enums;
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
        // A review is considered "bad" when its rating is strictly below this value.
        private const int DefaultBadReviewRatingThreshold = 3;

        // A user is flagged (AlerteTooBadReviews) when their bad-review count is strictly above this value.
        private const int DefaultTooManyBadReviewsThreshold = 5;

        private readonly UserManager<User> _userManager;            // Seller manager
        private readonly SignInManager<User> _signInManager;        // Sign-in manager
        private readonly EcoscolarDbContext _context;               // Database context
        private readonly UserMapper _userMapper;                    // User mapper for converting between entities and DTOs
        private readonly AbuseReportMapper _abuseReportMapper;
        private readonly int _badReviewRatingThreshold;             // Rating below which a review counts as "bad"
        private readonly int _tooManyBadReviewsThreshold;           // Bad-review count above which a user is flagged

        public AdminService(UserManager<User> userManager, EcoscolarDbContext dbContext, SignInManager<User> signInManager, UserMapper userMapper, AbuseReportMapper abuseReportMapper, IConfiguration configuration)
        {
            _userManager = userManager;
            _context = dbContext;
            _signInManager = signInManager;
            _userMapper = userMapper;
            _abuseReportMapper = abuseReportMapper;
            _badReviewRatingThreshold = configuration.GetValue("BusinessSettings:BadReviewRatingThreshold", DefaultBadReviewRatingThreshold);
            _tooManyBadReviewsThreshold = configuration.GetValue("BusinessSettings:TooManyBadReviewsThreshold", DefaultTooManyBadReviewsThreshold);
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

            // Count, per reviewed user, the reviews received with a rating below the bad-review threshold.
            // Done as a single grouped aggregate to avoid loading every review into memory.
            var badReviewCounts = await _context.Reviews
                .Where(r => r.Rating < _badReviewRatingThreshold)
                .GroupBy(r => r.ReviewedId)
                .Select(g => new { ReviewedId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ReviewedId, g => g.Count);

            var userDtos = new List<UserResponse>();

            foreach (var item in users)
            {
                var badReviews = badReviewCounts.GetValueOrDefault(item.Id, 0);
                userDtos.Add(_userMapper.ToResponse(item) with
                {
					Roles = [.. await _userManager.GetRolesAsync(item)],
                    BadReviewsCount = badReviews,
                    AlerteTooBadReviews = badReviews > _tooManyBadReviewsThreshold
                });
            }
            return Result<List<UserResponse>>.Success(userDtos);
        }

        public async Task<Result<List<SupportTicketAdminDto>>> GetAllSupports(ClaimsPrincipal user)
        {
            if (!user.IsInRole("Admin"))
                return Result<List<SupportTicketAdminDto>>.Failure("Unauthorized access.", ErrorType.Unauthorized);
            var supports = await _context.SupportTickets
                .Include(s => s.User)
                .Include(s => s.Messages)
                .OrderByDescending(t => t.CreatedAt)
                .Select(m => new SupportTicketAdminDto(
                    m.Id,
                    m.Email,
                    m.Subject,
                    m.Message,
                    m.UserId,
                    m.CreatedAt,
                    // On mappe l'utilisateur vers un DTO simplifié ou on extrait juste les infos nécessaires
                    m.User != null ? new UserAdminDto(m.User.FirstName, m.User.LastName, m.User.Nickname, m.User.Email) : null,
                    // On projette chaque message de l'entité vers le DTO Message
                    m.Messages.Select(msg => new SupportTicketMessageAdminDto(
                        msg.Id,
                        msg.Body,
                        msg.IsFromSupport,
                        msg.CreatedAt
                    )).ToList()
                ))
                .ToListAsync();
            return Result<List<SupportTicketAdminDto>>.Success(supports);
        }

        public async Task<Result<SupportTicketMessageAdminDto>> AddTicketMessage(ClaimsPrincipal user, int ticketId, SupportTicketMessageRequestDto request)
        {
            if (!user.IsInRole("Admin"))
                return Result<SupportTicketMessageAdminDto>.Failure("Unauthorized access.", ErrorType.Unauthorized);

            var body = request.Message.Trim();
            if (string.IsNullOrWhiteSpace(body))
                return Result<SupportTicketMessageAdminDto>.Failure("Please enter a message.", ErrorType.Conflict);

            if (! await _context.SupportTickets.AnyAsync(t => t.Id == ticketId))
                return Result<SupportTicketMessageAdminDto>.Failure("Request not found.", ErrorType.NotFound);

            var message = new SupportTicketMessage
            {
                TicketId = ticketId,
                Body = body,
                IsFromSupport = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTicketMessages.Add(message);
            await _context.SaveChangesAsync();

            return Result<SupportTicketMessageAdminDto>.Success(new SupportTicketMessageAdminDto(
                message.Id,
                message.Body,
                message.IsFromSupport,
                message.CreatedAt));
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

            var updateResult = await _userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description);
                return Result<UserResponse>.Failure(errors);
            }

            var roles = await _userManager.GetRolesAsync(currentUser);

            return Result<UserResponse>.Success(_userMapper.ToResponse(currentUser) with { Roles = roles.ToArray() });
        }

        public async Task<Result<AdvertReadDto>> BlockAdvert(ClaimsPrincipal user, long advertId)
        {
            if (!user.IsInRole("Admin"))
                return Result<AdvertReadDto>.Failure("Unauthorized access.", ErrorType.Unauthorized);

            var currentAdvert = _context.Adverts.FirstOrDefault(a => a.AdvertId == advertId);

            if (currentAdvert == null)
                return Result<AdvertReadDto>.Failure("Advert not found.", ErrorType.NotFound);

            if(currentAdvert.Status == AdvertStatus.BLOCKED)
                return Result<AdvertReadDto>.Failure("Advert is already blocked.", ErrorType.Conflict);

            currentAdvert.Status = AdvertStatus.BLOCKED;

            await _context.SaveChangesAsync();

            AdvertReadDto advertReadDto = AdvertReadDto.FromEntity(currentAdvert);
            return Result<AdvertReadDto>.Success(advertReadDto);
        }

        public async Task<Result<List<AbuseReportAdminDto>>> GetAllAbuses(ClaimsPrincipal user)
        {
            if (!user.IsInRole("Admin"))
                return Result<List<AbuseReportAdminDto>>.Failure("Unauthorized access.", ErrorType.Unauthorized);

            var abuses = await _context.AbuseReports
                .Include(s => s.Reporter)
                .Include(s => s.TargetAdvert)
                .ThenInclude(a => a!.Seller)
                .Include(a => a.TargetComment)
                .ThenInclude(c => c!.Author)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return Result<List<AbuseReportAdminDto>>.Success(abuses.Select(a => _abuseReportMapper.ToAbuseReportAdminDto(a)).ToList());
        }
    }
}
