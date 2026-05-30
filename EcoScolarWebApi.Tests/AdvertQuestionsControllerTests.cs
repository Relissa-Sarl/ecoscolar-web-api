using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace EcoScolarWebApi.Tests.Controllers;

public class AdvertQuestionsControllerTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly UserManager<User> _userManagerMock;
	private readonly AdvertQuestionsController _controller;
	private readonly PublicCommentMapper _mapper = new();

	public AdvertQuestionsControllerTests()
	{
		var store = Substitute.For<IUserStore<User>>();
		_userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);

		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		_context = new EcoscolarDbContext(options);
		_controller = new AdvertQuestionsController(_context, _userManagerMock, _mapper)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(new ClaimsIdentity())
				}
			}
		};
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
	}

	[Fact]
	public async Task GetQuestions_ReturnsNotFound_WhenAdvertDoesNotExist()
	{
		var result = await _controller.GetQuestions(999);

		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task GetQuestions_ReturnsQuestions_WhenAdvertExists()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		var commenter = new User { Id = "commenter-1", UserName = "userx", Email = "userx@test.ch" };
		var advert = new TutoringAdvert
		{
			AdvertId = 1,
			Title = "Cours de maths",
			Description = "Test advert",
			Price = 20,
			Status = EcoScolarWebApi.Enums.AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow.AddDays(7),
			SellerId = seller.Id,
			Seller = seller,
			StudyLevel = "Lycée",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR
		};

		_context.Users.AddRange(seller, commenter);
		_context.Adverts.Add(advert);
		await _context.SaveChangesAsync();

		var question = new PublicComment
		{
			CommentId = 1,
			AdvertId = advert.AdvertId,
			Advert = advert,
			AuthorId = commenter.Id,
			Author = commenter,
			Content = "Est-ce disponible le week-end ?",
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};

		_context.PublicComments.Add(question);
		await _context.SaveChangesAsync();

		var result = await _controller.GetQuestions(advert.AdvertId);

		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var questions = okResult.Value.Should().BeAssignableTo<IEnumerable<QuestionResponseDTO>>().Subject.ToList();

		questions.Should().HaveCount(1);
		questions[0].CommentId.Should().Be(1);
		questions[0].AuthorId.Should().Be(commenter.Id);
		questions[0].Author.Should().Be(commenter.UserName);
		questions[0].Content.Should().Be("Est-ce disponible le week-end ?");
	}

	[Fact]
	public async Task AskQuestion_ReturnsUnauthorized_WhenUserIsMissing()
	{
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		var result = await _controller.AskQuestion(1, new QuestionRequestDTO("Ma question"));

		result.Result.Should().BeOfType<UnauthorizedResult>();
	}

	[Fact]
	public async Task AskQuestion_ReturnsNotFound_WhenAdvertDoesNotExist()
	{
		var user = new User { Id = "user-1", UserName = "user1", Email = "user1@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);

		var result = await _controller.AskQuestion(1, new QuestionRequestDTO("Ma question"));

		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task AskQuestion_CreatesQuestion_AndReturnsCreated()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		var asker = new User { Id = "user-1", UserName = "userx", Email = "userx@test.ch" };
		var advert = new TutoringAdvert
		{
			AdvertId = 1,
			Title = "Cours de maths",
			Description = "Test advert",
			Price = 20,
			Status = EcoScolarWebApi.Enums.AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow.AddDays(7),
			SellerId = seller.Id,
			Seller = seller,
			StudyLevel = "Lycée",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR
		};

		_context.Users.AddRange(seller, asker);
		_context.Adverts.Add(advert);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(asker);

		var result = await _controller.AskQuestion(advert.AdvertId, new QuestionRequestDTO("Ma question"));

		var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
		var response = created.Value.Should().BeAssignableTo<QuestionResponseDTO>().Subject;

		response.AuthorId.Should().Be(asker.Id);
		response.Author.Should().Be(asker.UserName);
		response.Content.Should().Be("Ma question");
		response.Answer.Should().BeEmpty();
		response.AnsweredAt.Should().BeNull();

		_context.PublicComments.Should().HaveCount(1);
		_context.PublicComments.Single().Content.Should().Be("Ma question");
	}

	[Fact]
	public async Task AnswerQuestion_ReturnsUnauthorized_WhenUserIsMissing()
	{
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		var result = await _controller.AnswerQuestion(1, 1, new AnswerRequestDTO("Oui"));

		result.Result.Should().BeOfType<UnauthorizedResult>();
	}

	[Fact]
	public async Task AnswerQuestion_ReturnsNotFound_WhenQuestionDoesNotExist()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		_context.Users.Add(seller);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(seller);

		var result = await _controller.AnswerQuestion(1, 99, new AnswerRequestDTO("Oui"));

		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task AnswerQuestion_ReturnsForbid_WhenCallerIsNotSeller()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		var otherUser = new User { Id = "user-2", UserName = "user2", Email = "user2@test.ch" };
		var advert = new TutoringAdvert
		{
			AdvertId = 1,
			Title = "Cours de maths",
			Description = "Test advert",
			Price = 20,
			Status = EcoScolarWebApi.Enums.AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow.AddDays(7),
			SellerId = seller.Id,
			Seller = seller,
			StudyLevel = "Lycée",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR
		};
		var question = new PublicComment
		{
			CommentId = 1,
			AdvertId = advert.AdvertId,
			Advert = advert,
			AuthorId = otherUser.Id,
			Author = otherUser,
			Content = "Est-ce disponible ?",
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};

		_context.Users.AddRange(seller, otherUser);
		_context.Adverts.Add(advert);
		_context.PublicComments.Add(question);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(otherUser);

		var result = await _controller.AnswerQuestion(advert.AdvertId, question.CommentId, new AnswerRequestDTO("Oui"));

		result.Result.Should().BeOfType<ForbidResult>();
	}

	[Fact]
	public async Task AnswerQuestion_ReturnsBadRequest_WhenAnswerAlreadyExists()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		var advert = new TutoringAdvert
		{
			AdvertId = 1,
			Title = "Cours de maths",
			Description = "Test advert",
			Price = 20,
			Status = EcoScolarWebApi.Enums.AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow.AddDays(7),
			SellerId = seller.Id,
			Seller = seller,
			StudyLevel = "Lycée",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR
		};
		var question = new PublicComment
		{
			CommentId = 1,
			AdvertId = advert.AdvertId,
			Advert = advert,
			AuthorId = "user-2",
			Content = "Est-ce disponible ?",
			Answer = "Oui",
			AnsweredAt = DateTime.UtcNow.AddHours(-1),
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};

		_context.Users.Add(seller);
		_context.Adverts.Add(advert);
		_context.PublicComments.Add(question);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(seller);

		var result = await _controller.AnswerQuestion(advert.AdvertId, question.CommentId, new AnswerRequestDTO("Nouvelle réponse"));

		var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
		badRequest.Value.Should().NotBeNull();
	}

	[Fact]
	public async Task AnswerQuestion_ReturnsOk_AndStoresAnswer_WhenSellerReplies()
	{
		var seller = new User { Id = "seller-1", UserName = "testuser", Email = "seller@test.ch" };
		var asker = new User { Id = "user-1", UserName = "userx", Email = "userx@test.ch" };
		var advert = new TutoringAdvert
		{
			AdvertId = 1,
			Title = "Cours de maths",
			Description = "Test advert",
			Price = 20,
			Status = EcoScolarWebApi.Enums.AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow.AddDays(7),
			SellerId = seller.Id,
			Seller = seller,
			StudyLevel = "Lycée",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = EcoScolarWebApi.Enums.LanguageEnum.FR
		};
		var question = new PublicComment
		{
			CommentId = 1,
			AdvertId = advert.AdvertId,
			Advert = advert,
			AuthorId = asker.Id,
			Author = asker,
			Content = "Est-ce disponible ?",
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};

		_context.Users.AddRange(seller, asker);
		_context.Adverts.Add(advert);
		_context.PublicComments.Add(question);
		await _context.SaveChangesAsync();

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(seller);

		var result = await _controller.AnswerQuestion(advert.AdvertId, question.CommentId, new AnswerRequestDTO("Oui, bien sûr"));

		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var response = okResult.Value.Should().BeAssignableTo<AnswerResponseDTO>().Subject;

		response.Answer.Should().Be("Oui, bien sûr");
		response.AnsweredAt.Should().NotBeNull();
		_context.PublicComments.Single().Answer.Should().Be("Oui, bien sûr");
		_context.PublicComments.Single().AnsweredAt.Should().NotBeNull();
	}
}
