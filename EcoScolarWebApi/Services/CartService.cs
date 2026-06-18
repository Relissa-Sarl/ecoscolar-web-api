using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Cart;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using EcoScolarWebApi.Enums;
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
                    .ThenInclude(a => a.Seller!)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var invalidItems = cartItems.Where(c => c.Advert == null || c.Advert.Status == AdvertStatus.SOLD).ToList();
            if (invalidItems.Any())
            {
                _context.CartItems.RemoveRange(invalidItems);
                await _context.SaveChangesAsync();
                cartItems = cartItems.Except(invalidItems).ToList();
            }

            var dtos = cartItems.Select(c =>
            {
                var pic = _context.Pictures.Where(p => p.PhysicalItemId == c.AdvertId).OrderBy(p => p.SortOrder).FirstOrDefault();
                var primaryImage = pic?.PublicUrl ?? pic?.Label;

                string type = c.Advert switch
                {
                    Book => "BOOK",
                    PhysicalItem => "PRODUCT",
                    TutoringAdvert => "SERVICE",
                    _ => "Advert"
                };

                return new CartItemDto
                {
                    AdvertId = c.AdvertId,
                    Title = c.Advert?.Title ?? string.Empty,
                    Price = c.Advert?.Price ?? 0,
                    SellerPseudo = c.Advert?.Seller?.Nickname ?? c.Advert?.Seller?.UserName ?? string.Empty,
                    Type = type,
                    PrimaryImage = primaryImage,
                    Status = c.Advert?.Status.ToString() ?? string.Empty
                };
            }).ToList();

            return Result<IEnumerable<CartItemDto>>.Success(dtos);
        }

        public async Task<Result<CartItemDto>> AddToCartAsync(string userId, AddToCartDto dto)
        {
            // Verify if advert exist
            var advert = await _context.Adverts
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.AdvertId == dto.AdvertId);

            if (advert == null)
            {
                return Result<CartItemDto>.Failure("L'annonce spécifiée n'existe pas.", ErrorType.NotFound);
            }

            // Tutoring is sold via a dedicated reservation flow (hours + escrow), never through the cart.
            if (advert is TutoringAdvert)
            {
                return Result<CartItemDto>.Failure("Les cours d'appui se réservent directement depuis l'annonce, pas via le panier.", ErrorType.Conflict);
            }
            if (advert.Status == AdvertStatus.SOLD)
            {
                return Result<CartItemDto>.Failure("Cet article a déjà été vendu.", ErrorType.Conflict);
            }

            // Veryfa if user try to add it own advert
            if (advert.SellerId == userId)
            {
                return Result<CartItemDto>.Failure("Vous ne pouvez pas ajouter votre propre annonce à votre panier.", ErrorType.Conflict);
            }

            // Verify if advert is already in the cart
            var alreadyInCart = await _context.CartItems
                .AnyAsync(c => c.UserId == userId && c.AdvertId == dto.AdvertId);

            if (alreadyInCart)
            {
                return Result<CartItemDto>.Failure("Cet article est déjà dans votre panier.", ErrorType.Conflict);
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
            var primaryPic = await _context.Pictures
                .Where(p => p.PhysicalItemId == dto.AdvertId)
                .OrderBy(p => p.SortOrder)
                .FirstOrDefaultAsync();
            var primaryImage = primaryPic?.PublicUrl ?? primaryPic?.Label;

            string type = advert switch
            {
                Book => "BOOK",
                PhysicalItem => "PRODUCT",
                TutoringAdvert => "SERVICE",
                _ => "Advert"
            };

            var resultDto = new CartItemDto
            {
                AdvertId = cartItem.AdvertId,
                Title = advert.Title,
                Price = advert.Price,
                SellerPseudo = advert.Seller?.Nickname ?? advert.Seller?.UserName ?? string.Empty,
                Type = type,
                PrimaryImage = primaryImage,
                Status = advert.Status.ToString()
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