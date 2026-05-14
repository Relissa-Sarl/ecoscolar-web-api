using EcoscolarWebApi.Controllers;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace EcoscolarWebApi.Tests.Controllers;

public class UsersControllerTests
{
	private readonly UserManager<User> _userManagerMock;
	private readonly UsersController _controller;

	public UsersControllerTests()
	{
		var store = Substitute.For<IUserStore<User>>();
		_userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!); // UserManager requires a lot of dependencies, we can mock them all with NSubstitute

		// Simulate the dependency injection of UserManager into the UsersController
		_controller = new UsersController(_userManagerMock);
	}

	#region Tests pour RegisterCustom

	[Fact]
	public async Task RegisterCustom_ShouldReturnOk_WhenCreationIsSuccessful()
	{
		// Arrange
		var request = new UserDto { Email = "test@ecoscolar.ch", Password = "Password123!", FirstName = "Alexis", LastName = "Rojas" };

		_userManagerMock.CreateAsync(Arg.Any<User>(), request.Password)
			.Returns(IdentityResult.Success);

		// Act
		var result = await _controller.RegisterCustom(request);

		// Assert
		result.Should().BeOfType<OkObjectResult>();
	}

	[Fact]
	public async Task RegisterCustom_ShouldReturnBadRequest_WhenCreationFails()
	{
		// Arrange
		var request = new UserDto { Email = "invalid", Password = "123" };
		var errors = new[] { new IdentityError { Description = "Password too short" } };

		_userManagerMock.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
			.Returns(IdentityResult.Failed(errors));

		// Act
		var result = await _controller.RegisterCustom(request);

		// Assert
		var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
		badRequest.Value.Should().BeEquivalentTo(errors);
	}

	#endregion

	#region Tests pour GetMyProfile

	[Fact]
	public async Task GetMyProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
	{
		// Arrange
		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User?)null);

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task GetMyProfile_ShouldReturnOk_WithUserData_WhenUserExists()
	{
		// Arrange
		var existingUser = new User
		{
			Id = "guid-123",
			UserName = "alexis@etml.ch",
			Email = "alexis@etml.ch",
			FirstName = "alexis",
			LastName = "Rojas"
		};

		_userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

		// Act
		var result = await _controller.GetMyProfile();

		// Assert
		var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

		// Vérification que les données retournées correspondent à l'utilisateur existant
		okResult.Value.Should().BeEquivalentTo(new
		{
			Id = "guid-123",
			UserName = "alexis@etml.ch",
			FirstName = "Alexis",
			LastName = "Rojas",
			Email = "alexis@etml.ch"
		});
	}

	#endregion
}