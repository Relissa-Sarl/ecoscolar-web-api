
using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/adverts/{advertId}/questions")]
[ApiController]
[Authorize]
public class AdvertQuestionsController(EcoscolarDbContext context, UserManager<User> userManager, PublicCommentMapper mapper) : Controller
{
	private readonly EcoscolarDbContext _context = context;
	private readonly UserManager<User> _userManager = userManager;
	private readonly PublicCommentMapper _mapper = mapper;

	[HttpGet]
	public async Task<ActionResult<IEnumerable<QuestionResponseDTO>>> GetQuestions(long advertId)
	{
		var advertExists = await _context.Adverts.AnyAsync(a => a.AdvertId == advertId);
		if (!advertExists)
			return NotFound();

		var questions = await _mapper.ProjectToQuestionResponses(
		_context.PublicComments.Where(q => q.AdvertId == advertId)).ToListAsync();

		return Ok(questions);
	}
	[HttpPost]
	public async Task<ActionResult<QuestionResponseDTO>> AskQuestion(long advertId, [FromBody] QuestionRequestDTO question)
	{
		var user = await _userManager.GetUserAsync(User);
		if (user is null) return Unauthorized();

		if (!await _context.Adverts.AnyAsync(a => a.AdvertId == advertId)) return NotFound();

		var publicComment = new PublicComment
		{
			AdvertId = advertId,
			Content = question.Content,
			CreatedAt = DateTime.UtcNow,
			AuthorId = user.Id,
			Author = user
		};

		_context.PublicComments.Add(publicComment);
		await _context.SaveChangesAsync();

		return CreatedAtAction(nameof(GetQuestions), new { advertId }, _mapper.ToQuestionResponse(publicComment));
	}
	[HttpPost("{questionId}/answers")]
	public async Task<ActionResult<QuestionResponseDTO>> AnswerQuestion(long advertId, long questionId, [FromBody] AnswerRequestDTO answer)
	{
		var user = await _userManager.GetUserAsync(User);
		if (user is null) return Unauthorized();

		var publicQuestion = await _context.PublicComments
			.Include(q => q.Advert)
			.FirstOrDefaultAsync(q => q.CommentId == questionId && q.AdvertId == advertId);

		if (publicQuestion is null) return NotFound();
		if (publicQuestion.Advert.SellerId != user.Id) return Forbid();

		if (!string.IsNullOrWhiteSpace(publicQuestion.Answer))
			return BadRequest(new { message = "An answer has already been posted for this question and cannot be modified." });

		publicQuestion.Answer = answer.Content;
		publicQuestion.AnsweredAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(_mapper.ToAnswerResponse(publicQuestion));
	}
}
