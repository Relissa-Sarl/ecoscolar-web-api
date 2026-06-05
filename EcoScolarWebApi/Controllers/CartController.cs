using Asp.Versioning;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Cart;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoScolarWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly UserManager<User> _userManager;

        public CartController(ICartService cartService, UserManager<User> userManager)
        {
            _cartService = cartService;
            _userManager = userManager;
        }

        // GET: api/v1/cart
        [HttpGet]
        public async Task<IActionResult> GetCartItems() 
        {
            var userId = _userManager.GetUserId(User);
            // If user is null or does not exist, then unauthorized
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _cartService.GetCartItemsAsync(userId);
            // 200 ok
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            // 403 error
            return BadRequest(new { result.Errors });
        }

        // POST: api/v1/cart
        [HttpPost]
        public async Task<IActionResult> PostCartItem([FromBody] AddToCartDto dto)
        {
            var userId = _userManager.GetUserId(User);
            // If user is null or does not exist, then unauthorized
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _cartService.AddToCartAsync(userId, dto);
            // 201 created
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetCartItems), null, result.Data);
            }

            return result.ErrorType switch
            {
                // 404 error not found
                ErrorType.NotFound => NotFound(new { result.Errors }),
                // 403 error bad request
                _ => BadRequest(new { result.Errors })
            };
        }

        // DELETE: api/v1/cart/{advertId}
        [HttpDelete("{advertId}")]
        public async Task<IActionResult> DeleteCartItem(long advertId) 
        {
            var userId = _userManager.GetUserId(User);
            // If user is null or does not exist, then unauthorized
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _cartService.RemoveFromCartAsync(userId, advertId);
            // 204 np content
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return result.ErrorType switch
            {
                // 404 error
                ErrorType.NotFound => NotFound(new { result.Errors }),
                // 403 error
                _ => BadRequest(new { result.Errors })
            };
        }
    }
}