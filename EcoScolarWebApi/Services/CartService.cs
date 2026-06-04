using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Cart;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Services
{
    public class CartService : ICartService
    {
        private readonly EcoscolarDbContext _context;

        public CartService(EcoscolarDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<CartItemDto>>> GetCartItemsAsync(string userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Advert)
                    .ThenInclude(a => a.User)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var dtos = cartItems.Select(c =>
            {
                var primaryImage = _context.Pictures.FirstOrDefault(p => p.AdvertId == c.AdvertId)?.Label;
                
                string type = c.Advert switch
                {
                    PhysicalItem => "PhysicalItem",
                    TutoringAdvert => "TutoringAdvert",
                    _ => "Advert"
                };

                return new CartItemDto
                {
                    AdvertId = c.AdvertId,
                    Title = c.Advert?.Title ?? string.Empty,
                    Price = c.Advert?.Price ?? 0,
                    SellerPseudo = c.Advert?.User?.Nickname ?? c.Advert?.User?.UserName ?? string.Empty,
                    Type = type,
                    PrimaryImage = primaryImage
                };
            });

            return Result<IEnumerable<CartItemDto>>.Success(dtos);
        }

        public async Task<Result<CartItemDto>> AddToCartAsync(string userId, AddToCartDto dto)
        {
            // Verify if advert exist
            var advert = await _context.Adverts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdvertId == dto.AdvertId);

            if (advert == null)
            {
                return Result<CartItemDto>.Failure("L'annonce spécifiée n'existe pas.", ErrorType.NotFound);
            }

            // Veryfa if user try to add it own advert
            if (advert.UserId == userId)
            {
                return Result<CartItemDto>.Failure("Vous ne pouvez pas ajouter votre propre annonce à votre panier.", ErrorType.Invalid);
            }

            // Verify if advert is already in the cart
            var alreadyInCart = await _context.CartItems
                .AnyAsync(c => c.UserId == userId && c.AdvertId == dto.AdvertId);

            if (alreadyInCart)
            {
                return Result<CartItemDto>.Failure("Cet article est déjà dans votre panier.", ErrorType.Invalid);
            }

            // Create and save element
            var cartItem = new CartItem
            {
                UserId = userId,
                AdvertId = dto.AdvertId
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            // Get main picture of the advert
            var primaryImage = await _context.Pictures
                .Where(p => p.AdvertId == dto.AdvertId)
                .Select(p => p.Label)
                .FirstOrDefaultAsync();

            string type = advert switch
            {
                PhysicalItem => "PhysicalItem",
                TutoringAdvert => "TutoringAdvert",
                _ => "Advert"
            };

            var resultDto = new CartItemDto
            {
                AdvertId = cartItem.AdvertId,
                Title = advert.Title,
                Price = advert.Price,
                SellerPseudo = advert.User?.Nickname ?? advert.User?.UserName ?? string.Empty,
                Type = type,
                PrimaryImage = primaryImage
            };

            return Result<CartItemDto>.Success(resultDto);
        }

        public async Task<Result> RemoveFromCartAsync(string userId, long advertId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.AdvertId == advertId);

            if (cartItem == null)
            {
                return Result.Failure("L'article n'est pas présent dans votre panier.", ErrorType.NotFound);
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Result.Success();
        }
    }
}
