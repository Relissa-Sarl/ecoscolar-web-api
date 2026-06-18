using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using System.Security.Claims;
using EcoScolarWebApi.Data;
using Microsoft.EntityFrameworkCore;
using EcoScolarWebApi.Models;
using Asp.Versioning;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize] // Ces routes nécessitent un utilisateur connecté
public class MeController : ControllerBase
{
    private readonly EcoscolarDbContext _context;

    public MeController(EcoscolarDbContext context)
    {
        _context = context;
    }

    [HttpGet("purchases")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPurchases()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid session." });

        var purchases = await _context.Transactions
            .Where(t => t.BuyerId == userId)
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .Include(t => t.Advert)
                .ThenInclude(a => (a as PhysicalItem)!.Pictures)
            .ToListAsync();

        var transactionIds = purchases.Select(t => t.TransactionId).ToList();
        var reviews = await _context.Reviews
            .Where(r => transactionIds.Contains(r.TransactionId) && r.ReviewedRole == ReviewedRole.SELLER)
            .ToDictionaryAsync(r => r.TransactionId);

        var purchaseDtos = purchases.Select(t =>
        {
            reviews.TryGetValue(t.TransactionId, out var review);
            return new PurchaseReadDto(
                t.TransactionId.ToString(),
                t.AdvertId.ToString(),
                t.Advert!.Title,
                t.Advert.Price,
                t.Date,
                t.Status.ToString(),
                GetPrimaryImage(t.Advert),
                t.Advert.Seller?.Nickname ?? t.Advert.Seller?.UserName ?? "Anonyme",
                review != null ? new ReviewDto(review.Rating, review.Comment) : null,
                t.OrderNumber,
                GetAdvertType(t.Advert)
            );
        }).ToList();

        return Ok(purchaseDtos);
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IEnumerable<AdvertReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySales()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid session." });

        var adverts = await _context.Adverts
            .Where(a => a.SellerId == userId)
            .Include(a => a.Seller)
            .ToListAsync();

        var physicalItemIds = adverts.OfType<PhysicalItem>().Select(item => item.AdvertId).ToList();
        if (physicalItemIds.Count > 0)
        {
            await _context.Pictures
                .Where(p => physicalItemIds.Contains(p.PhysicalItemId))
                .LoadAsync();
        }

        var advertIds = adverts.Select(a => a.AdvertId).ToList();
        var transactions = await _context.Transactions
            .Include(t => t.Buyer)
            .Where(t => advertIds.Contains(t.AdvertId) && t.Status != TransactionStatus.PENDING)
            .ToListAsync();

        var transactionsByAdvert = transactions
            .GroupBy(t => t.AdvertId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        var transactionIds = transactions.Select(t => t.TransactionId).ToList();
        var reviews = await _context.Reviews
            .Where(r => transactionIds.Contains(r.TransactionId) && r.ReviewedRole == ReviewedRole.SELLER)
            .ToDictionaryAsync(r => r.TransactionId);

        var dtos = new List<AdvertReadDto>();
        foreach (var advert in adverts)
        {
            var advertTransactions = transactionsByAdvert.GetValueOrDefault(advert.AdvertId) ?? [];

            // Tutoring: one card per active package (several students can book the same advert).
            if (advert is TutoringAdvert)
            {
                var activePackages = advertTransactions
                    .Where(t => t.Status is not TransactionStatus.CANCELLED and not TransactionStatus.COMPLETED)
                    .OrderByDescending(t => SellerTransactionPriority(t.Status))
                    .ThenByDescending(t => t.Date)
                    .ToList();

                if (activePackages.Count == 0)
                {
                    dtos.Add(AdvertReadDto.FromEntity(advert));
                    continue;
                }

                foreach (var activePackage in activePackages)
                {
                    dtos.Add(MapSaleDto(advert, activePackage, reviews));
                }

                continue;
            }

            var transaction = advertTransactions
                .OrderByDescending(t => t.Date)
                .FirstOrDefault();

            dtos.Add(MapSaleDto(advert, transaction, reviews));
        }

        return Ok(dtos);
    }

    private static AdvertReadDto MapSaleDto(
        Advert advert,
        Transaction? transaction,
        IReadOnlyDictionary<long, Review> reviews)
    {
        ReviewDto? reviewDto = null;
        if (transaction != null && reviews.TryGetValue(transaction.TransactionId, out var review))
            reviewDto = new ReviewDto(review.Rating, review.Comment);

        var dto = AdvertReadDto.FromEntity(advert, reviewDto);
        if (transaction == null)
            return dto;

        return dto with
        {
            BuyerName = transaction.Buyer?.Nickname ?? transaction.Buyer?.UserName ?? "Anonyme",
            TransactionId = transaction.TransactionId,
            TransactionStatus = transaction.Status.ToString()
        };
    }

    private static int SellerTransactionPriority(TransactionStatus status) => status switch
    {
        TransactionStatus.PAID_WAITING_ACCEPTANCE => 100,
        TransactionStatus.PAID_WAITING_COMPLETION => 90,
        TransactionStatus.DISPUTED => 80,
        TransactionStatus.PAID_WAITING_SHIPPING => 70,
        TransactionStatus.SHIPPED => 60,
        _ => 0
    };

    private static string? GetPrimaryImage(Advert advert)
    {
        if (advert is PhysicalItem physicalItem && physicalItem.Pictures != null && physicalItem.Pictures.Any())
        {
            var pic = physicalItem.Pictures.OrderBy(p => p.SortOrder).First();
            return pic.PublicUrl ?? pic.Label;
        }
        return null;
    }

    [HttpPost("purchases/{transactionId}/cancel")]
    public async Task<IActionResult> CancelPurchase(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid session." });

        var transaction = await _context.Transactions
            .Include(t => t.Advert)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            return NotFound();

        if (transaction.BuyerId != userId)
            return Forbid();

        if (transaction.Status != TransactionStatus.PAID_WAITING_SHIPPING)
            return BadRequest(new { message = "La transaction ne peut être annulée que si elle est en attente d'expédition." });

        transaction.Status = TransactionStatus.CANCELLED;

        if (transaction.Advert != null)
        {
            transaction.Advert.Status = AdvertStatus.ACTIVE;
        }

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("sales/{transactionId}/confirm-shipping")]
    public async Task<IActionResult> ConfirmShipping(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid session." });

        var transaction = await _context.Transactions
            .Include(t => t.Advert)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            return NotFound();

        if (transaction.Advert.SellerId != userId)
            return Forbid();

        if (transaction.Status != TransactionStatus.PAID_WAITING_SHIPPING)
            return BadRequest(new { message = "La transaction n'est pas en attente d'expédition." });

        transaction.Status = TransactionStatus.SHIPPED;
        transaction.ShippedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("sales/{advertId}/renew")]
    public async Task<IActionResult> RenewAdvert(long advertId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid session." });

        var advert = await _context.Adverts.FirstOrDefaultAsync(a => a.AdvertId == advertId);

        if (advert == null)
            return NotFound();

        if (advert.SellerId != userId)
            return Forbid();

        if (advert.Status == AdvertStatus.SOLD || advert.Status == AdvertStatus.BLOCKED)
            return BadRequest(new { message = "Cette annonce ne peut pas être renouvelée." });

        advert.CreatedAt = DateTime.UtcNow;
        advert.NotificationDate = DateTime.UtcNow;
        advert.Status = AdvertStatus.ACTIVE;

        await _context.SaveChangesAsync();

        return Ok();
    }

    private static string GetAdvertType(Advert advert) => advert switch
    {
        Book => "BOOK",
        PhysicalItem => "PRODUCT",
        TutoringAdvert => "SERVICE",
        _ => "PRODUCT"
    };
}