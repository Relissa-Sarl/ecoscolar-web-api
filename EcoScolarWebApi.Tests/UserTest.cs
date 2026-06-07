using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using EcoScolarWebApi.Mappers;
using Xunit;

namespace EcoScolarWebApi.Tests;

public class UsersControllerTests
{
	private readonly UserManager<User> _userManagerMock;
	private readonly IUserService _userServiceMock;
	private readonly EcoscolarDbContext _context;
	private readonly UsersController _controller;
	private readonly ReviewMapper _reviewMapper;
	private readonly UserMapper _userMapper;

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
        _reviewMapper = new ReviewMapper();

		_userMapper = new UserMapper();

        // Simulate the dependency injection of UserManager and DbContext into the UsersController
        _controller = new UsersController(_userServiceMock, _userManagerMock, _context, _reviewMapper);
	}


	#region Tests pour GetMyProfile

	[Fact]
	public async Task GetMyProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userServiceMock.GetCurrentUserProfileAsync(Arg.Any<ClaimsPrincipal>())
			.Returns(Result<UserResponse>.Failure(new[] { "Seller not found" }, ErrorType.NotFound));

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task GetMyProfile_ShouldReturnOk_WithUserData_WhenUserExists()
	{
		// Arrange
		var UserResponse = new UserResponse(
			"guid-123",
			"alexis",
			"Alexis",
			"Rojas",
			"alexis@etml.ch",
			true,
			false,
			new List<SpokenLanguageDto>(),
			null,
			"2000-01-01",
			["User"]
		);

		_userServiceMock.GetCurrentUserProfileAsync(Arg.Any<ClaimsPrincipal>())
			.Returns(Result<UserResponse>.Success(UserResponse));

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

		// Vérification que les données retournées correspondent à l'utilisateur existant
		okResult.Value.Should().BeEquivalentTo(UserResponse);
	}

	[Fact]
	public async Task GetMyProfile_ShouldReturnUnauthorized_WhenSessionInvalid()
	{
		_userServiceMock.GetCurrentUserProfileAsync(Arg.Any<ClaimsPrincipal>())
			.Returns(Result<UserResponse>.Failure("Invalid session.", ErrorType.Unauthorized));

		var result = await _controller.GetMyProfile();

		result.Should().BeOfType<UnauthorizedObjectResult>();
	}

    #endregion

    #region Tests pour AnonymizeProfileAsync (Service)

    [Fact]
    public async Task AnonymizeProfileAsync_ShouldReturnUnauthorized_WhenUserIdIsMissing()
    {
        // Arrange
        var store = Substitute.For<IUserStore<User>>();
        var userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
        var signInManagerMock = Substitute.For<SignInManager<User>>(userManagerMock, Substitute.For<IHttpContextAccessor>(), Substitute.For<IUserClaimsPrincipalFactory<User>>(), null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        using var context = new EcoscolarDbContext(options);

        var userService = new UserService(userManagerMock, context, signInManagerMock, _userMapper);

        userManagerMock.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns((string?)null);

        // Act
        var result = await userService.AnonymizeProfileAsync(new ClaimsPrincipal());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task AnonymizeProfileAsync_ShouldAnonymizeDataAndSignOut_WhenUserExists()
    {
        // Arrange
        var store = Substitute.For<IUserStore<User>>();
        var userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
        var signInManagerMock = Substitute.For<SignInManager<User>>(userManagerMock, Substitute.For<IHttpContextAccessor>(), Substitute.For<IUserClaimsPrincipalFactory<User>>(), null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

        using var context = new EcoscolarDbContext(options);
        var userService = new UserService(userManagerMock, context, signInManagerMock, _userMapper);

        var existingUser = new User
        {
            Id = "guid-delete-me",
            FirstName = "Damien",
            LastName = "Loup",
            Nickname = "Sikties",
            DateOfBirth = "1995-05-21",
            IsOnboarded = true
        };

        context.Users.Add(existingUser);
        var favorite = new UserFavorite { UserId = existingUser.Id, AdvertId = 99 };
        context.UserFavorites.Add(favorite);
        await context.SaveChangesAsync();

        userManagerMock.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns(existingUser.Id);

        userManagerMock.Users.Returns(context.Users);

        userManagerMock.NormalizeEmail(Arg.Any<string>()).Returns("@DELETED.ECOSCOLAR.COM");
        userManagerMock.NormalizeName(Arg.Any<string>()).Returns("@DELETED.ECOSCOLAR.COM");

        userManagerMock.SetEmailAsync(Arg.Any<User>(), Arg.Any<string>())
        .Returns(Task.FromResult(IdentityResult.Success));

        userManagerMock.SetUserNameAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));

        userManagerMock.UpdateAsync(Arg.Any<User>()).Returns(IdentityResult.Success);

        signInManagerMock.SignOutAsync().Returns(Task.CompletedTask);

        // Act
        var result = await userService.AnonymizeProfileAsync(new ClaimsPrincipal());

        // Assert
        result.IsSuccess.Should().BeTrue();

        existingUser.FirstName.Should().NotBe("Damien");
        existingUser.LastName.Should().NotBe("Loup");
        existingUser.Nickname.Should().StartWith("DeletedUser_");
        existingUser.DateOfBirth.Should().Be("1995-01-01");
        existingUser.IsOnboarded.Should().BeFalse();

        var favoriteInDb = await context.UserFavorites
        .FirstOrDefaultAsync(f => f.UserId == existingUser.Id && f.AdvertId == 99);

        favoriteInDb.Should().BeNull();

        // Vérification Identity native
        existingUser.NormalizedEmail.Should().Contain("@DELETED.ECOSCOLAR.COM");
        existingUser.NormalizedUserName.Should().Contain("@DELETED.ECOSCOLAR.COM");

        // Vérification de la déconnexion forcée
        await signInManagerMock.Received(1).SignOutAsync();
    }

    #endregion

    #region Tests pour DeleteMyProfile

    [Fact]
    public async Task DeleteMyProfile_ShouldReturnOk_WhenAnonymizationSucceeds()
    {
        // Arrange
        _userServiceMock.AnonymizeProfileAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<bool>.Success(true));

        // Act
        var result = await _controller.DeleteMyProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "The account has successfully got anonymized" });
    }

    [Fact]
    public async Task DeleteMyProfile_ShouldReturnUnauthorized_WhenSessionIsInvalid()
    {
        // Arrange
        _userServiceMock.AnonymizeProfileAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<bool>.Failure("SESSION_INVALID", ErrorType.Unauthorized));

        // Act
        var result = await _controller.DeleteMyProfile();

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMyProfile_ShouldReturnNotFound_WhenUserSessionExpired()
    {
        // Arrange
        _userServiceMock.AnonymizeProfileAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<bool>.Failure("SESSION_EXPIRED", ErrorType.NotFound));

        // Act
        var resultDelete = await _controller.DeleteMyProfile();

        // Assert
        resultDelete.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Tests pour UpdateFullProfile

    [Fact]
	public async Task UpdateFullProfile_ShouldReturnOk_WhenUpdateSucceeds()
	{
		var UserResponse = new UserResponse(
			"guid-update",
			"nick",
			"First",
			"Last",
			"update@example.com",
			true,
			false,
			[new SpokenLanguageDto("FR", "Native")],
			new LocationReadDto("1000", "Lausanne", "Vaud"),
			"2000-01-01",
			["User"]
		);

		_userServiceMock.UpdateProfileAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserUpdateDto>())
			.Returns(Result<UserResponse>.Success(UserResponse));

		var result = await _controller.UpdateFullProfile(new UserUpdateDto(
			"nick", "First", "Last", "1000", "2000-01-01",
			[new SpokenLanguageDto("FR", "Native")]));

		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		okResult.Value.Should().BeEquivalentTo(UserResponse);
	}

	[Fact]
	public async Task UpdateFullProfile_ShouldReturnNotFound_WhenUserMissing()
	{
		_userServiceMock.UpdateProfileAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserUpdateDto>())
			.Returns(Result<UserResponse>.Failure("User not found", ErrorType.NotFound));

		var result = await _controller.UpdateFullProfile(new UserUpdateDto(
			"nick", "First", "Last", "1000", "2000-01-01",
			[new SpokenLanguageDto("FR", "Native")]));

		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task UpdateFullProfile_ShouldReturnBadRequest_WhenPostalCodeInvalid()
	{
		_userServiceMock.UpdateProfileAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserUpdateDto>())
			.Returns(Result<UserResponse>.Failure("Invalid postal code", ErrorType.BadRequest));

		var result = await _controller.UpdateFullProfile(new UserUpdateDto(
			"nick", "First", "Last", "9999", "2000-01-01",
			[new SpokenLanguageDto("FR", "Native")]));

		result.Should().BeOfType<BadRequestObjectResult>();
	}

	#endregion

	#region Tests pour GetUserProfile (public)

	[Fact]
	public async Task GetUserProfile_ShouldReturnOk_WhenProfileIsPublic()
	{
		var publicDto = new UserPublicReadDto("guid-public", "public_nick");
		_userServiceMock.GetPublicProfileAsync("guid-public")
			.Returns(Result<UserPublicReadDto>.Success(publicDto));

		var result = await _controller.GetUserProfile("guid-public");

		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		okResult.Value.Should().BeEquivalentTo(publicDto);
	}

	[Fact]
	public async Task GetUserProfile_ShouldReturnNotFound_WhenProfileNotPublic()
	{
		_userServiceMock.GetPublicProfileAsync(Arg.Any<string>())
			.Returns(Result<UserPublicReadDto>.Failure("User not found or profile is not public yet.", ErrorType.NotFound));

		var result = await _controller.GetUserProfile("missing");

		result.Should().BeOfType<NotFoundObjectResult>();
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
			SellerId = existingUser.Id,
			Seller = existingUser,
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
			SellerId = existingUser.Id,
			Seller = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			Condition = PhysicalItemCondition.LIKE_NEW
		};
		var serviceAdvert = new TutoringAdvert
		{
			AdvertId = 3,
			Title = "Math tutoring",
			Description = "Algebra",
			Price = 30,
			SellerId = existingUser.Id,
			Seller = existingUser,
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
			Advert = bookAdvert,
			User = existingUser
		};
		var favoritePhysical = new UserFavorite
		{
			UserId = existingUser.Id,
			AdvertId = physicalItemAdvert.AdvertId,
			Advert = physicalItemAdvert,
			User = existingUser
		};
		var favoriteService = new UserFavorite
		{
			UserId = existingUser.Id,
			AdvertId = serviceAdvert.AdvertId,
			Advert = serviceAdvert,
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
		returnedFavorites.Should().Contain(a => a.Id == bookAdvert.AdvertId && a.Type == "BOOK");
		returnedFavorites.Should().Contain(a => a.Id == physicalItemAdvert.AdvertId && a.Type == "PRODUCT");
		returnedFavorites.Should().Contain(a => a.Id == serviceAdvert.AdvertId && a.Type == "SERVICE");

		// Cleanup for in memory db persistence between tests
		_context.UserFavorites.RemoveRange(favoriteBook, favoritePhysical, favoriteService);
		await _context.SaveChangesAsync();
	}

	#endregion

	#region Tests for DeleteSearchAlert

	[Fact]
	public async Task DeleteSearchAlert_ShouldReturnNotFound_WhenAlertDoesNotExist()
	{
		var existingUser = new User { Id = "guid-alert-del-0", UserName = "alert@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var result = await _controller.DeleteSearchAlert(999);

		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task DeleteSearchAlert_ShouldReturnNoContent_WhenAlertOwnedByUser()
	{
		var existingUser = new User { Id = "guid-alert-del-1", UserName = "alert@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		_context.SearchAlerts.Add(new SearchAlert
		{
			UserId = existingUser.Id,
			AdvertSearch = "Biologie",
			AdvertType = CatalogAdvertTypeCodes.Books
		});
		await _context.SaveChangesAsync();

		var alertId = _context.SearchAlerts.First().ResearchId;

		var result = await _controller.DeleteSearchAlert(alertId);

		result.Should().BeOfType<NoContentResult>();

		var inDb = await _context.SearchAlerts.FindAsync(alertId);
		inDb.Should().BeNull();
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
			SellerId = "other",
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
			SellerId = "other",
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

	#region Tests for CreateSearchAlert

	[Fact]
	public async Task CreateSearchAlert_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		var result = await _controller.CreateSearchAlert(new CreateSearchAlertDto { Q = "Biologie" });

		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task CreateSearchAlert_ShouldReturnBadRequest_WhenNoCriteria()
	{
		var existingUser = new User { Id = "guid-alert-0", UserName = "alert@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var result = await _controller.CreateSearchAlert(new CreateSearchAlertDto());

		result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task CreateSearchAlert_ShouldReturnCreated_WhenCriteriaValid()
	{
		var existingUser = new User { Id = "guid-alert-1", UserName = "alert@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		var result = await _controller.CreateSearchAlert(new CreateSearchAlertDto { Q = "Calculatrice" });

		var created = result.Should().BeOfType<ObjectResult>().Subject;
		created.StatusCode.Should().Be(StatusCodes.Status201Created);
		var alertDto = created.Value.Should().BeOfType<SearchAlertReadDto>().Subject;
		alertDto.Q.Should().Be("Calculatrice");
		alertDto.Id.Should().BeGreaterThan(0);

		var inDb = await _context.SearchAlerts.FirstOrDefaultAsync(a => a.UserId == existingUser.Id);
		inDb.Should().NotBeNull();
		inDb!.AdvertSearch.Should().Be("Calculatrice");
	}

	#endregion

	#region Tests for GetMySearchAlerts

	[Fact]
	public async Task GetMySearchAlerts_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		var result = await _controller.GetMySearchAlerts();

		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task GetMySearchAlerts_ShouldReturnOnlyCurrentUserAlerts_OrderedByResearchIdDesc()
	{
		var userA = new User { Id = "guid-alert-get-a", UserName = "a@test.ch" };
		var userB = new User { Id = "guid-alert-get-b", UserName = "b@test.ch" };
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(userA);

		_context.SearchAlerts.AddRange(
			new SearchAlert { UserId = userA.Id, AdvertSearch = "Old", AdvertType = CatalogAdvertTypeCodes.Books },
			new SearchAlert { UserId = userA.Id, AdvertSearch = "New", AdvertType = CatalogAdvertTypeCodes.Books },
			new SearchAlert { UserId = userB.Id, AdvertSearch = "Other", AdvertType = CatalogAdvertTypeCodes.Books }
		);
		await _context.SaveChangesAsync();

		var result = await _controller.GetMySearchAlerts();

		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
		var alerts = okResult.Value.Should().BeAssignableTo<IEnumerable<SearchAlertReadDto>>().Subject.ToList();
		alerts.Should().HaveCount(2);
		alerts[0].Q.Should().Be("New");
		alerts[1].Q.Should().Be("Old");
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
			SellerId = existingUser.Id,
			Seller = existingUser,
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
			SellerId = existingUser.Id,
			Seller = existingUser,
			Status = AdvertStatus.ACTIVE,
			CreatedAt = DateTime.UtcNow,
			NotificationDate = DateTime.UtcNow,
			Condition = PhysicalItemCondition.LIKE_NEW
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
		returnedAdverts.Should().Contain(a => a.Id == advert1.AdvertId && a.Type == "BOOK");
		returnedAdverts.Should().Contain(a => a.Id == advert2.AdvertId && a.Type == "PRODUCT");
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

	#region Tests for GetUserReviews

	[Fact]
	public async Task GetUserReviews_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		// Act
		var result = await _controller.GetUserReviews("non-existent-user");

		// Assert
		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task GetUserReviews_ShouldReturnOkWithReviews_WhenUserExists()
	{
		// Arrange
		var userId = "test-user-id";
		var reviewerId = "reviewer-user-id";

		var user = new User { Id = userId, Nickname = "test_user", FirstName = "Test", LastName = "User" };
		var reviewer = new User { Id = reviewerId, Nickname = "reviewer", FirstName = "Review", LastName = "Er" };

		var transaction = new Transaction
		{
			TransactionId = 10,
			BuyerId = userId,
			AdvertId = 101,
			Advert = new Book
			{
				AdvertId = 101,
				Title = "Book Title",
				Description = "Book Desc",
				SellerId = reviewerId,
				Seller = reviewer,
				ISBN = "12345",
				Author = "John",
				Publisher = "Pub",
				Edition = "1st",
				WrittenLanguage = Enums.LanguageEnum.FR
			}
		};

		var review = new Review
		{
			ReviewId = 1,
			Comment = "Great service!",
			Rating = 5,
			Date = DateTime.UtcNow,
			ReviewerId = reviewerId,
			Reviewer = reviewer,
			ReviewedId = userId,
			Reviewed = user,
			TransactionId = 10,
			Transaction = transaction,
			ReviewedRole = ReviewedRole.BUYER
		};

		_context.Users.AddRange(user, reviewer);
		_context.Transactions.Add(transaction);
		_context.Reviews.Add(review);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetUserReviews(userId);

		// Assert
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var returnedReviews = okResult.Value.Should().BeAssignableTo<IEnumerable<ReviewResponseDTO>>().Subject.ToList();
		returnedReviews.Should().HaveCount(1);
		returnedReviews[0].ReviewId.Should().Be(1);
		returnedReviews[0].Comment.Should().Be("Great service!");
		returnedReviews[0].Rating.Should().Be(5);
		returnedReviews[0].ReviewedId.Should().Be(userId);
		returnedReviews[0].ReviewerId.Should().Be(reviewerId);

		// Cleanup
		_context.Reviews.Remove(review);
		_context.Transactions.Remove(transaction);
		_context.Users.RemoveRange(user, reviewer);
		await _context.SaveChangesAsync();
	}

	#endregion
}