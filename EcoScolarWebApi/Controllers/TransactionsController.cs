using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.DTOs.Transactions;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Security.Cryptography;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class TransactionsController(EcoscolarDbContext context, UserManager<User> userManager, ReviewMapper reviewMapper, IConfiguration configuration, IEmailSenderService emailSenderService) : ControllerBase
{
    private readonly EcoscolarDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ReviewMapper _reviewMapper = reviewMapper;
    private readonly IConfiguration _configuration = configuration;
    private readonly IEmailSenderService _emailSenderService = emailSenderService;

    [HttpPost("{transactionId}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewResponseDTO>>> CreateReview(long transactionId, [FromBody] ReviewRequestDTO review)
    {
        var transactionUserIds = await _context.Transactions
            .Where(t => t.TransactionId == transactionId)
            .Select(t => new TransactionUserIdsDto(t.BuyerId, t.Advert.SellerId))
            .FirstOrDefaultAsync();

        // If the transaction doesn't exist, return 404 Not Found
        if (transactionUserIds is null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        string? reviewedUserId = null;
        ReviewedRole reviewedRole;

        // Check if the current user is either the buyer or the seller in this transaction
        if (user.Id == transactionUserIds.BuyerId)
        {
            reviewedUserId = transactionUserIds.SellerId;
            reviewedRole = ReviewedRole.SELLER;
        }
        else if (user.Id == transactionUserIds.SellerId)
        {
            reviewedUserId = transactionUserIds.BuyerId;
            reviewedRole = ReviewedRole.BUYER;
        }
        else
            return Forbid();

        var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.TransactionId == transactionId && r.ReviewerId == user.Id);
        if (alreadyReviewed)
            return Conflict(new { message = "A review already exists for this transaction from the current user." });

        var newReview = new Review
        {
            Comment = review.Comment,
            Rating = review.Rating,
            ReviewerId = user.Id,
            ReviewedId = reviewedUserId,
            TransactionId = transactionId,
            ReviewedRole = reviewedRole
        };

        _context.Reviews.Add(newReview);
        await _context.SaveChangesAsync();

        // Reload the review of the transaction (bidirectional reviews)
        var reviews = await _reviewMapper.ProjectToReviewResponseDTOs(
                            _context.Reviews.Where(r => r.TransactionId == transactionId))
                            .ToListAsync();

        return CreatedAtAction(nameof(CreateReview), new { transactionId }, reviews);
    }

    [HttpPut("{transactionId}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(long transactionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var transaction = await _context.Transactions
            .Include(t => t.Advert)
                .ThenInclude(a => a.Seller)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction is null)
            return NotFound();

        if (transaction.BuyerId != user.Id)
            return Forbid();

        transaction.Status = TransactionStatus.COMPLETED;
        if (transaction.Advert != null)
        {
            transaction.Advert.Status = AdvertStatus.SOLD;

            // Trigger Stripe transfer if the seller has a connected account
            if (!string.IsNullOrEmpty(transaction.Advert.Seller?.StripeAccountId))
            {
                try
                {
                    var transferOptions = new Stripe.TransferCreateOptions
                    {
                        Amount = (long)(transaction.Advert.Price * 100), // en centimes
                        Currency = "chf",
                        Destination = transaction.Advert.Seller.StripeAccountId,
                        TransferGroup = "COMMANDE_ID_789", // Same as defined in checkout
                    };

                    var transferService = new Stripe.TransferService();
                    await transferService.CreateAsync(transferOptions);
                }
                catch (Stripe.StripeException ex)
                {
                    // Log the exception or handle it, but allow the transaction to be completed
                    Console.WriteLine($"Stripe transfer failed: {ex.Message}");
                }
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{transactionId}/dispute")]
    public async Task<IActionResult> DisputePurchase(long transactionId, [FromBody] EcoScolarWebApi.DTOs.Transactions.DisputeRequestDto request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction is null)
            return NotFound();

        if (transaction.BuyerId != user.Id)
            return Forbid();

		if (transaction.Status != TransactionStatus.SHIPPED)
			return BadRequest(new { message = "You can only dispute a shipped order." });

		var dispute = new Dispute
		{
			TransactionId = transactionId,
			Reason = request.Reason,
			Description = request.Description,
			Status = EcoScolarWebApi.Enums.TicketStatus.Pending,
			Date = DateTime.UtcNow
		};

        transaction.Status = TransactionStatus.DISPUTED;

        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransactions([FromBody] CreateTransactionRequestDto request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        if (request.AdvertIds == null || request.AdvertIds.Count == 0)
        {
            return BadRequest(new { message = "AdvertIds are required." });
        }

		var existingOrderNumber = await _context.Transactions
			.Where(t => request.AdvertIds.Contains(t.AdvertId) && t.OrderNumber != null)
			.Select(t => t.OrderNumber)
			.FirstOrDefaultAsync();

		var orderNumber = existingOrderNumber ?? await GenerateUniqueOrderNumberAsync();

        var createdTransactions = new List<Transaction>();
        var soldAdverts = new List<Advert>();

        foreach (var advertId in request.AdvertIds)
        {
            // Check if a transaction already exists for this advert
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.AdvertId == advertId);

			if (existingTransaction != null)
			{
				if (string.IsNullOrWhiteSpace(existingTransaction.OrderNumber))
				{
					existingTransaction.OrderNumber = orderNumber;
				}
				createdTransactions.Add(existingTransaction);
				continue;
			}

            var advert = await _context.Adverts.FindAsync(advertId);
            if (advert == null)
            {
                return NotFound(new { message = $"Advert with ID {advertId} not found." });
            }

            // Calculate platform fee (5% of price, rounded to 2 decimal places)
            var platformFee = Math.Round(advert.Price * 0.05m, 2);

			var newTransaction = new Transaction
			{
				AdvertId = advertId,
				BuyerId = user.Id,
				Date = DateTime.UtcNow,
				Status = TransactionStatus.PAID_WAITING_SHIPPING,
				PlatformFee = platformFee,
				OrderNumber = orderNumber,
				StripeSessionId = request.StripeSessionId,
				BuyerConsent = false,
				SellerConsent = false
			};

            // Update the advert status to SOLD
            advert.Status = AdvertStatus.SOLD;
            _context.Entry(advert).State = EntityState.Modified;

            _context.Transactions.Add(newTransaction);
            createdTransactions.Add(newTransaction);
            soldAdverts.Add(advert);
        }

		await _context.SaveChangesAsync();

		var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
		foreach (var advert in soldAdverts)
		{
			if (advert.Seller == null)
			{
				await _context.Entry(advert).Reference(a => a.Seller).LoadAsync();
			}

			if (advert.Seller != null && !string.IsNullOrEmpty(advert.Seller.Email))
			{
				var allSoldLink = $"{baseUrl.TrimEnd('/')}/me/sales?from=profile";
				try
				{
					await _emailSenderService.SendItemSoldEmailAsync(advert.Seller, advert, allSoldLink);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Error: Failed to send sale notification email :{e.Message}");
				}
			}
		}

        return Ok(createdTransactions.Select(t => new {
            t.TransactionId,
            t.AdvertId,
            t.BuyerId,
            t.Status,
            t.Date,
            t.PlatformFee,
            t.StripeSessionId
        }));
    }
		var existingOrderNumber = await _context.Transactions
			.Where(t => request.AdvertIds.Contains(t.AdvertId) && t.OrderNumber != null)
			.Select(t => t.OrderNumber)
			.FirstOrDefaultAsync();

		var orderNumber = existingOrderNumber ?? await GenerateUniqueOrderNumberAsync();

		var createdTransactions = new List<Transaction>();

		foreach (var advertId in request.AdvertIds)
		{
			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.AdvertId == advertId);

			if (existingTransaction != null)
			{
				if (string.IsNullOrWhiteSpace(existingTransaction.OrderNumber))
				{
					existingTransaction.OrderNumber = orderNumber;
				}
				createdTransactions.Add(existingTransaction);
				continue;
			}

			var advert = await _context.Adverts.FindAsync(advertId);
			if (advert == null)
			{
				return NotFound(new { message = $"Advert with ID {advertId} not found." });
			}

			var platformFee = Math.Round(advert.Price * 0.05m, 2);

			var newTransaction = new Transaction
			{
				AdvertId = advertId,
				BuyerId = user.Id,
				Date = DateTime.UtcNow,
				Status = TransactionStatus.PAID_WAITING_SHIPPING,
				PlatformFee = platformFee,
				OrderNumber = orderNumber,
				StripeSessionId = request.StripeSessionId,
				BuyerConsent = false,
				SellerConsent = false
			};

			advert.Status = AdvertStatus.SOLD;
			_context.Entry(advert).State = EntityState.Modified;

			_context.Transactions.Add(newTransaction);
			createdTransactions.Add(newTransaction);
		}

		await _context.SaveChangesAsync();

		return Ok(createdTransactions.Select(t => new {
			t.TransactionId,
			t.AdvertId,
			t.BuyerId,
			t.Status,
			t.Date,
			t.PlatformFee,
			t.OrderNumber,
			t.StripeSessionId
		}));
	}

	private async Task<string> GenerateUniqueOrderNumberAsync()
	{
		string orderNumber;
		do
		{
			orderNumber = $"ECO-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
		}
		while (await _context.Transactions.AnyAsync(t => t.OrderNumber == orderNumber));
		return orderNumber;
	}
}

public record TransactionUserIdsDto(string BuyerId, string SellerId);