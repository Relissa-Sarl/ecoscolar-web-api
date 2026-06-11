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
                review != null ? new ReviewDto(review.Rating, review.Comment) : null
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

        var sales = await _context.Adverts
            .Where(a => a.SellerId == userId)
            .Include(a => a.Seller)
            // Left join with transactions to get the buyer if sold
            .Select(a => new
            {
                Advert = a,
                Transaction = _context.Transactions.Include(t => t.Buyer).FirstOrDefault(t => t.AdvertId == a.AdvertId)
            })
            .ToListAsync();

        // We need to fetch pictures separately to avoid complex queries or cartesian explosions
        var physicalItemIds = sales.Select(s => s.Advert).OfType<PhysicalItem>().Select(item => item.AdvertId).ToList();
        if (physicalItemIds.Any())
        {
            await _context.Pictures
                .Where(p => physicalItemIds.Contains(p.PhysicalItemId))
                .LoadAsync();
        }

        var transactionIds = sales
            .Where(s => s.Transaction != null)
            .Select(s => s.Transaction!.TransactionId)
            .ToList();

        var reviews = await _context.Reviews
            .Where(r => transactionIds.Contains(r.TransactionId) && r.ReviewedRole == ReviewedRole.SELLER)
            .ToDictionaryAsync(r => r.TransactionId);

        var dtos = sales.Select(s =>
        {
            ReviewDto? reviewDto = null;
            if (s.Transaction != null && reviews.TryGetValue(s.Transaction.TransactionId, out var review))
            {
                reviewDto = new ReviewDto(review.Rating, review.Comment);
            }

            var dto = AdvertReadDto.FromEntity(s.Advert, reviewDto);
            // If we have a transaction, we update the buyer name in the record
            if (s.Transaction?.Buyer != null)
            {
                // Use the buyer's Nickname for anonymisation; fall back to UserName if Nickname is not yet set.
                dto = dto with { BuyerName = s.Transaction.Buyer.Nickname ?? s.Transaction.Buyer.UserName ?? "Anonyme" };
            }
            return dto;
        }).ToList();

        return Ok(dtos);
    }

    private static string? GetPrimaryImage(Advert advert)
    {
        if (advert is PhysicalItem physicalItem && physicalItem.Pictures != null && physicalItem.Pictures.Any())
        {
            return physicalItem.Pictures.First().Label;
        }
        return null;
    }
}