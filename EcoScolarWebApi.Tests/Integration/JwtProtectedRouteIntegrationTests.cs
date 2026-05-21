using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// T8-2 · intégration route protégée JWT (UserService + Identity).
/// </summary>
public class JwtProtectedRouteIntegrationTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly EcoscolarDbContext _context;
	private readonly UserManager<User> _userManager;
	private readonly UserService _userService;

	public JwtProtectedRouteIntegrationTests()
	{
		_provider = IntegrationTestIdentityHelper.CreateIdentityProvider(out _context);
		_userManager = _provider.GetRequiredService<UserManager<User>>();
		var signInManager = _provider.GetRequiredService<SignInManager<User>>();
		_userService = new UserService(_userManager, _context, signInManager);
	}

	[Fact]
	public async Task GetMyProfile_WithoutAuth_ReturnsUnauthorized()
	{
		var result = await _userService.GetCurrentUserProfileAsync(new ClaimsPrincipal());

		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.Unauthorized);
	}

	[Fact]
	public async Task GetMyProfile_WithAuthenticatedUser_ReturnsProfile()
	{
		const string email = "jwt.integration@example.com";

		var user = new User
		{
			UserName = email,
			Email = email,
			FirstName = "Jwt",
			LastName = "Test"
		};
		(await _userManager.CreateAsync(user, "Password123!")).Succeeded.Should().BeTrue();

		var claims = new ClaimsPrincipal(new ClaimsIdentity([
			new Claim(ClaimTypes.NameIdentifier, user.Id)
		], "TestAuth"));

		var result = await _userService.GetCurrentUserProfileAsync(claims);

		result.IsSuccess.Should().BeTrue();
		result.Data!.Email.Should().Be(email);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		_provider.Dispose();
	}
}
