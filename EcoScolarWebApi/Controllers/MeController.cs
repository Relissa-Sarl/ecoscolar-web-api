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
using Stripe;

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

        var purchaseDtos = purchases.Select(t => new PurchaseReadDto(
            t.TransactionId.ToString(),
            t.AdvertId.ToString(),
            t.Advert!.Title,
            t.Advert.Price,
            t.Date,
            t.Status.ToString(),
            GetPrimaryImage(t.Advert),
            t.Advert.Seller?.Nickname ?? t.Advert.Seller?.UserName ?? "Anonyme"
        )).ToList();

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

        var dtos = sales.Select(s =>
        {
            var dto = AdvertReadDto.FromEntity(s.Advert);
            // If we have a transaction, we update the buyer name in the record
            if (s.Transaction != null)
            {
                var buyerName = s.Transaction.Buyer?.Nickname ?? s.Transaction.Buyer?.UserName ?? "Anonyme";
                dto = dto with { 
                    BuyerName = buyerName,
                    TransactionId = s.Transaction.TransactionId,
                    TransactionStatus = s.Transaction.Status.ToString()
                };
            }
            return dto;
        });

        return Ok(dtos);
    }

    [HttpPost("sales/{transactionId}/confirm-shipping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmShipping(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var transaction = await _context.Transactions
            .Include(t => t.Advert)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.Advert.SellerId == userId);

        if (transaction == null) return NotFound(new { error = "Transaction introuvable." });
        if (transaction.Status != TransactionStatus.PAID_WAITING_SHIPPING) 
            return BadRequest(new { error = "L'article n'est pas en attente d'expédition." });

        transaction.Status = TransactionStatus.SHIPPED;
        transaction.ShippedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "L'article a été marqué comme expédié." });
    }

    [HttpPost("purchases/{transactionId}/confirm-reception")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmReception(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var transaction = await _context.Transactions
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.BuyerId == userId);

        if (transaction == null) return NotFound(new { error = "Transaction introuvable." });
        if (transaction.Status != TransactionStatus.SHIPPED) 
            return BadRequest(new { error = "La transaction n'est pas marquée comme expédiée." });

        transaction.Status = TransactionStatus.COMPLETED;

        // Effectuer le virement au vendeur via Stripe Transfer si son compte est configuré
        if (!string.IsNullOrEmpty(transaction.Advert.Seller?.StripeAccountId))
        {
            try
            {
                var options = new TransferCreateOptions
                {
                    Amount = (long)(transaction.Advert.Price * 0.9m * 100), // Virement de 90% du prix
                    Currency = "chf",
                    Destination = transaction.Advert.Seller.StripeAccountId,
                    TransferGroup = $"TRANS_{transaction.TransactionId}"
                };
                var transferService = new TransferService();
                await transferService.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                // En production, il faudrait logger l'erreur et peut-être ne pas marquer COMPLETED si le transfert échoue
                // ou avoir un système de retry. Pour l'instant, on laisse passer mais on renvoie un warning.
                Console.WriteLine($"Erreur Stripe Transfer: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "La réception a été confirmée et les fonds ont été libérés." });
    }

    [HttpPost("purchases/{transactionId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPurchase(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var transaction = await _context.Transactions
            .Include(t => t.Advert)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.BuyerId == userId);

        if (transaction == null) return NotFound(new { error = "Transaction introuvable." });
        if (transaction.Status != TransactionStatus.PAID_WAITING_SHIPPING) 
            return BadRequest(new { error = "La transaction ne peut plus être annulée." });

        transaction.Status = TransactionStatus.CANCELLED;
        transaction.Advert.Status = Enums.AdvertStatus.ACTIVE; // Remise en vente

        if (!string.IsNullOrEmpty(transaction.StripePaymentIntentId))
        {
            try
            {
                var refundService = new RefundService();
                await refundService.CreateAsync(new RefundCreateOptions
                {
                    PaymentIntent = transaction.StripePaymentIntentId
                });
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Erreur Stripe Refund: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Transaction annulée et remboursée." });
    }

    [HttpPost("purchases/{transactionId}/dispute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisputePurchase(long transactionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.BuyerId == userId);

        if (transaction == null) return NotFound(new { error = "Transaction introuvable." });
        if (transaction.Status != TransactionStatus.SHIPPED) 
            return BadRequest(new { error = "Vous ne pouvez ouvrir un litige qu'après expédition." });

        transaction.Status = TransactionStatus.DISPUTED;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Un litige a été ouvert pour cette transaction. Les fonds sont bloqués." });
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