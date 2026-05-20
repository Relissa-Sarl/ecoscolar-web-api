using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.AdvertDtos;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace EcoScolarWebApi.Tests.Controllers;

public class UsersControllerTests
{
	private readonly UserManager<User> _userManagerMock;
	private readonly IUserService _userServiceMock;
	private readonly EcoscolarDbContext _context;
	private readonly UsersController _controller;

	public UsersControllerTests()
	{
		var store = Substitute.For<IUserStore<User>>();
		_userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!); // UserManager requires a lot of dependencies, we can mock them all with NSubstitute

		// Setup InMemory database context
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);

		_userServiceMock = Substitute.For<IUserService>();

		// Simulate the dependency injection of UserManager and DbContext into the UsersController
		_controller = new UsersController(_userServiceMock, _userManagerMock, _context);
	}


	#region Tests pour GetMyProfile

	[Fact]
	public async Task GetMyProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userServiceMock.GetCurrentUserProfileAsync(Arg.Any<ClaimsPrincipal>())
			.Returns(Result<UserReadDto>.Failure(new[] { "User not found" }, ErrorType.NotFound));

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task GetMyProfile_ShouldReturnOk_WithUserData_WhenUserExists()
	{
		// Arrange
		var userReadDto = new UserReadDto(
			"guid-123",
			"alexis",
			"Alexis",
			"Rojas",
			"alexis@etml.ch",
			null,
			"2000-01-01",
			true,
			new List<SpokenLanguageDto>()
		);

		_userServiceMock.GetCurrentUserProfileAsync(Arg.Any<ClaimsPrincipal>())
			.Returns(Result<UserReadDto>.Success(userReadDto));

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

		// Vérification que les données retournées correspondent à l'utilisateur existant
		okResult.Value.Should().BeEquivalentTo(userReadDto);
	}

	#endregion

	#region Tests for GetMyFavorites
	[Fact]
	public async Task GetMyFavorites_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		// Act
		var result = await _controller.GetMyFavorites();

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task GetMyFavorites_ShouldReturnOk_WithData_WhenUserExistsAndHasFavorites()
	{
		// Arrange
		var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var bookAdvert = new Book
		{
			AdvertId = 1,
			Title = "Book Title",
			Description = "Book Descr",
			Price = 10,
			UserId = existingUser.Id,
			User = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			ISBN = "12345",
			Author = "John",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = Enums.LanguageEnum.FR
		};
		var physicalItemAdvert = new PhysicalItem
		{
			AdvertId = 2,
			Title = "Guitar",
			Description = "Acoustic",
			Price = 120,
			UserId = existingUser.Id,
			User = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			Condition = Condition.LIKE_NEW
		};
		var serviceAdvert = new AdvertService
		{
			AdvertId = 3,
			Title = "Math tutoring",
			Description = "Algebra",
			Price = 30,
			UserId = existingUser.Id,
			User = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			StudyLevel = "High School",
			SubjectId = 1,
			SchoolGradeId = 1,
			TeachingLanguage = Enums.LanguageEnum.FR
		};

		var favoriteBook = new UserFavorite
		{
			UserId = existingUser.Id,
			AdvertId = bookAdvert.AdvertId,
			Adverts = bookAdvert,
			User = existingUser
		};
		var favoritePhysical = new UserFavorite
		{
			UserId = existingUser.Id,
			AdvertId = physicalItemAdvert.AdvertId,
			Adverts = physicalItemAdvert,
			User = existingUser
		};
		var favoriteService = new UserFavorite
		{
			UserId = existingUser.Id,
			AdvertId = serviceAdvert.AdvertId,
			Adverts = serviceAdvert,
			User = existingUser
		};

		_context.UserFavorites.AddRange(favoriteBook, favoritePhysical, favoriteService);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetMyFavorites();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		var returnedFavorites = okResult.Value as IEnumerable<AdvertReadDto>;
		returnedFavorites.Should().NotBeNull();
		returnedFavorites.Should().HaveCount(3);
		returnedFavorites.Should().Contain(a => a.id == bookAdvert.AdvertId && a.type == "BOOK");
		returnedFavorites.Should().Contain(a => a.id == physicalItemAdvert.AdvertId && a.type == "PRODUCT");
		returnedFavorites.Should().Contain(a => a.id == serviceAdvert.AdvertId && a.type == "SERVICE");

		// Cleanup for in memory db persistence between tests
		_context.UserFavorites.RemoveRange(favoriteBook, favoritePhysical, favoriteService);
		await _context.SaveChangesAsync();
	}

	#endregion

	#region Tests pour ToggleFavorite

	[Fact]
	public async Task ToggleFavorite_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		// Act
		var result = await _controller.ToggleFavorite(1);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task ToggleFavorite_ShouldReturnNotFound_WhenAdvertDoesNotExist()
	{
		// Arrange
		var existingUser = new User { Id = "guid-toggle" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		// Act
		var result = await _controller.ToggleFavorite(999); // ID that doesn't exist

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task ToggleFavorite_ShouldAddFavorite_WhenNotInFavorites()
	{
		// Arrange
		var existingUser = new User { Id = "guid-toggle-1" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var advert = new Book
		{
			AdvertId = 2,
			Title = "Another book",
			Description = "Desc",
			UserId = "other",
			ISBN = "12345",
			Author = "John",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = Enums.LanguageEnum.FR
		};
		_context.Adverts.Add(advert);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.ToggleFavorite(2);

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		okResult.Value.Should().BeEquivalentTo(new { AdvertId = "2", IsFavorite = true });

		var favoriteInDb = await _context.UserFavorites.FirstOrDefaultAsync(u => u.UserId == existingUser.Id && u.AdvertId == 2);
		favoriteInDb.Should().NotBeNull();
	}

	[Fact]
	public async Task ToggleFavorite_ShouldRemoveFavorite_WhenAlreadyInFavorites()
	{
		// Arrange
		var existingUser = new User { Id = "guid-toggle-2" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var advert = new Book
		{
			AdvertId = 3,
			Title = "ToBeDeleted",
			Description = "Desc",
			UserId = "other",
			ISBN = "12345",
			Author = "John",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = Enums.LanguageEnum.FR
		};
		_context.Adverts.Add(advert);

		var favorite = new UserFavorite { UserId = existingUser.Id, AdvertId = 3 };
		_context.UserFavorites.Add(favorite);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.ToggleFavorite(3);

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		okResult.Value.Should().BeEquivalentTo(new { AdvertId = "3", IsFavorite = false });

		var favoriteInDb = await _context.UserFavorites.FirstOrDefaultAsync(u => u.UserId == existingUser.Id && u.AdvertId == 3);
		favoriteInDb.Should().BeNull();
	}

	#endregion

	#region Tests for GetMyAdverts
	[Fact]
    public async Task GetMyAdverts_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
        // Arrange
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

        // Act
        var result = await _controller.GetMyAdverts();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

	[Fact]
	public async Task GetMyAdverts_ShouldReturnOk_WithData_WhenUserExistsAndHasAdverts()
	{
		// Arrange
		var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
		var advert1 = new Book
		{
			AdvertId = 1,
			Title = "Book Title",
			Description = "Book Descr",
			Price = 10,
			UserId = existingUser.Id,
			User = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			ISBN = "12345",
			Author = "John",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = Enums.LanguageEnum.FR
		};
		var advert2 = new PhysicalItem
		{
			AdvertId = 2,
			Title = "Guitar",
			Description = "Acoustic",
			Price = 120,
			UserId = existingUser.Id,
			User = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			Condition = Condition.LIKE_NEW
		};
		_context.Adverts.AddRange(advert1, advert2);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetMyAdverts();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		var returnedAdverts = okResult.Value as IEnumerable<AdvertReadDto>;
		returnedAdverts.Should().NotBeNull();
		returnedAdverts.Should().HaveCount(2);
		returnedAdverts.Should().Contain(a => a.id == advert1.AdvertId && a.type == "BOOK");
		returnedAdverts.Should().Contain(a => a.id == advert2.AdvertId && a.type == "PRODUCT");
	}

	[Fact]
	public async Task GetMyAdverts_ShouldReturnOk_WithEmptyList_WhenUserExistsButHasNoAdverts()
	{
		// Arrange
		var existingUser = new User { Id = "guid-456", UserName = "jane_doe", FirstName = "Jane", LastName = "Doe" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		// Act
		var result = await _controller.GetMyAdverts();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		var returnedAdverts = okResult.Value as IEnumerable<AdvertReadDto>;
		returnedAdverts.Should().NotBeNull();
		returnedAdverts.Should().BeEmpty();
	}
    #endregion
    }