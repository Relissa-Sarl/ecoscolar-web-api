using Docker.DotNet.Models;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EcoScolarWebApi.Controllers
{

    /// <summary>
    /// Controller responsible for handling admin-related API endpoints, such as retrieving and updating admin profiles, managing admin accounts, and other admin-related operations. 
    /// This controller interacts with the IUserService to perform business logic related to admins and utilizes the UserManager for identity management tasks. It also uses the EcoscolarDbContext for database interactions when necessary.
    /// </summary>
    /// <param name="adminService">The user service for handling user-related operations</param>
    /// <param name="userManager">The user manager for handling identity management tasks</param>
    /// <param name="context">The database context for handling database interactions</param>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AdminsController(IAdminService adminService, UserManager<User> userManager, EcoscolarDbContext context) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly IAdminService _adminService = adminService;            // Seller service for handling user-related operations
        private readonly EcoscolarDbContext _context = context;


        #region admin
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            // Pass the HTTP session's Seller directly to the service
            var result = await _adminService.GetAllUsers(User);

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

        [HttpGet("supports")]
        public async Task<IActionResult> GetAllSupports()
        {
            // Pass the HTTP session's Seller directly to the service
            var result = await _adminService.GetAllSupports(User);

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
        [HttpPost("supports/{id}/message")]
        public async Task<IActionResult> AddTicketMessage(int id, [FromBody] SupportTicketMessageRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _adminService.AddTicketMessage(User, id, request);

            if (result.IsSuccess)
                return StatusCode(StatusCodes.Status201Created, result.Data);

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

        [HttpPatch("{userId}/ban")]
        public async Task<IActionResult> BanUser(string userId)
        {
            // Pass the HTTP session's Seller directly to the service
            var result = await _adminService.BanUserToggle(User, userId);

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

        [HttpPatch("{advertId}/block")]
        public async Task<IActionResult> BlockAdvert(long advertId)
        {
            var result = await _adminService.BlockAdvert(User, advertId);

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

        [HttpGet("abuses")]
        public async Task<IActionResult> GetAllAbuses()
        {
            var result = await _adminService.GetAllAbuses(User);

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

        [HttpGet("flagged-users")]
        public async Task<IActionResult> GetAllFlaggedUsers()
        {
            var result = await _adminService.GetFlaggedUsers(User);

            if (result.IsSuccess)
                return Ok(result.Data);

            return result.ErrorType switch
            {
                ErrorType.Unauthorized => Unauthorized(new { result.Errors }),
                _ => BadRequest(new { result.Errors })
            };
        }
        
        [HttpPatch("abuses/{id}/status")]
        public async Task<IActionResult> ChangeAbuseStatus(int id, [FromBody] AbuseStatusRequestDto status)
        {
            var result = await _adminService.ChangeAbuseStatus(User, id, status);

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

        [HttpDelete("abuses/{id}")]
        public async Task<IActionResult> DeleteFlag(int id)
        {
            var result = await _adminService.DeleteAbuse(User, id);

            // If successful, return 200 OK along with the user's data
            if (result.IsSuccess)
                return Ok(result);

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

        #endregion
    }
}
