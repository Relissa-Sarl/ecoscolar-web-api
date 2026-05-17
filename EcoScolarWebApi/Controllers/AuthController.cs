using EcoscolarWebApi.Services.Contracts;
using EcoscolarWebApi.Utils;
using EcoscolarWebApi.Utils.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    /// <summary>
    /// Defines API endpoints for user authentication operations such as registration and login.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;     // User service instance reference

        /// <summary>
        /// Initializes a new instance of the AuthController class using the specified user service.
        /// </summary>
        /// <param name="userService">The user service to be used for authentication and user-related operations. Cannot be null.</param>
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }
    }
}