using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Cart;

namespace EcoScolarWebApi.Services.Contracts
{
    /// <summary>
    /// Contract for the shopping cart service.
    /// </summary>
    public interface ICartService
    {
        /// <summary>
        /// Retrieves all items in the user's cart.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A collection of DTOs representing the items in the cart.</returns>
        Task<Result<IEnumerable<CartItemDto>>> GetCartItemsAsync(string userId);

        /// <summary>
        /// Adds an item to the user's cart.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="dto">The DTO containing the advert ID to add.</param>
        /// <returns>The added cart item DTO if successful.</returns>
        Task<Result<CartItemDto>> AddToCartAsync(string userId, AddToCartDto dto);

        /// <summary>
        /// Removes an item from the user's cart by its associated Advert ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="advertId">The ID of the advert to remove.</param>
        /// <returns>A successful result if removed, or failure description.</returns>
        Task<Result> RemoveFromCartAsync(string userId, long advertId);
    }
}
