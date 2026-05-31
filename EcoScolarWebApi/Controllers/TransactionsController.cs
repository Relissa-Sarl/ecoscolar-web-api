using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Reviews;
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
	public async Task<ActionResult<ReviewResponseDTO>> CreateReview(long transactionId, [FromBody] ReviewRequestDTO review)
	{
		var transactionUserIds = await _context.Transactions
			.Where(t => t.TransactionId == transactionId)
			.Select(t => new TransactionUserIdsDTO(t.BuyerId, t.Advert.SellerId))
			.FirstOrDefaultAsync();

		// If the transaction doesn't exist, return 404 Not Found
		if (transactionUserIds is null)
			return NotFound();

		var user = await _userManager.GetUserAsync(User);
		if (user is null)
			return Unauthorized();

		string? reviewedUserId = null;

		// Check if the current user is either the buyer or the seller in this transaction
		if (user.Id == transactionUserIds.BuyerId)
			reviewedUserId = transactionUserIds.SellerId;
		else if (user.Id == transactionUserIds.SellerId)
			reviewedUserId = transactionUserIds.BuyerId;
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
		};

		_context.Reviews.Add(newReview);
		await _context.SaveChangesAsync();

		return CreatedAtAction(nameof(CreateReview), new { transactionId }, _reviewMapper.ToReviewResponseDTO(newReview));
	}
}

public record TransactionUserIdsDTO(string BuyerId, string SellerId);
