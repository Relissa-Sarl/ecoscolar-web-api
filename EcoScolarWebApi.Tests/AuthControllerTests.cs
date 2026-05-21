using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EcoScolarWebApi.Tests.Controllers;

public class AuthControllerTests
{
	private readonly IUserService _userServiceMock;
	private readonly SignInManager<User> _signInManagerMock;
	private readonly AuthController _controller;

	public AuthControllerTests()
	{
		_userServiceMock = Substitute.For<IUserService>();

		var userStore = Substitute.For<IUserStore<User>>();
		var userManager = Substitute.For<UserManager<User>>(
			userStore, null!, null!, null!, null!, null!, null!, null!, null!);

		var contextAccessor = Substitute.For<IHttpContextAccessor>();
		contextAccessor.HttpContext.Returns(new DefaultHttpContext());

		_signInManagerMock = Substitute.For<SignInManager<User>>(
			userManager,
			contextAccessor,
			Substitute.For<IUserClaimsPrincipalFactory<User>>(),
			Substitute.For<IOptions<IdentityOptions>>(),
			Substitute.For<ILogger<SignInManager<User>>>(),
			Substitute.For<IAuthenticationSchemeProvider>(),
			Substitute.For<IUserConfirmation<User>>());

		_controller = new AuthController(_userServiceMock, _signInManagerMock);
	}

	[Fact]
	public async Task Logout_CallsSignOutAndReturnsOk()
	{
		var result = await _controller.Logout();

		await _signInManagerMock.Received(1).SignOutAsync();
		result.Should().BeOfType<OkResult>();
	}
}
