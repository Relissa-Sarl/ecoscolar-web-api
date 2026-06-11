using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class TransactionsController(EcoscolarDbContext context, UserManager<User> userManager, ReviewMapper reviewMapper) : ControllerBase
{
	private readonly EcoscolarDbContext _context = context;
	private readonly UserManager<User> _userManager = userManager;
	private readonly ReviewMapper _reviewMapper = reviewMapper;

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
			.FirstOrDefaultAsync(t => t.TransactionId == transactionId);

		if (transaction is null)
			return NotFound();

		if (transaction.BuyerId != user.Id)
			return Forbid();

		transaction.Status = TransactionStatus.COMPLETED;
		if (transaction.Advert != null)
		{
			transaction.Advert.Status = AdvertStatus.SOLD;
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

		var dispute = new Dispute
		{
			TransactionId = transactionId,
			Reason = request.Reason,
			Status = "OPEN",
			Date = DateTime.UtcNow
		};

		transaction.Status = TransactionStatus.DISPUTED;
		
		_context.Disputes.Add(dispute);
		await _context.SaveChangesAsync();

		return Ok();
	}
}

public record TransactionUserIdsDto(string BuyerId, string SellerId);
