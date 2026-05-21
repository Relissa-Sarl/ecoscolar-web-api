using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// Tests intégration UserService : profil, update, profil public.
/// </summary>
public class UserServiceIntegrationTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly EcoscolarDbContext _context;
	private readonly UserManager<User> _userManager;
	private readonly UserService _userService;

	public UserServiceIntegrationTests()
	{
		_provider = IntegrationTestIdentityHelper.CreateIdentityProvider(out _context);
		_userManager = _provider.GetRequiredService<UserManager<User>>();
		var signInManager = _provider.GetRequiredService<SignInManager<User>>();
		_userService = new UserService(_userManager, _context, signInManager);
	}

	[Fact]
	public async Task GetCurrentUserProfileAsync_ReturnsEmail_WhenUserExists()
	{
		const string email = "profile.service@example.com";
		var user = await CreateUserAsync(email);
		var principal = CreatePrincipal(user.Id);

		var result = await _userService.GetCurrentUserProfileAsync(principal);

		result.IsSuccess.Should().BeTrue();
		result.Data!.Email.Should().Be(email);
	}

	[Fact]
	public async Task GetCurrentUserProfileAsync_ReturnsUnauthorized_WhenPrincipalIsEmpty()
	{
		var result = await _userService.GetCurrentUserProfileAsync(new ClaimsPrincipal());

		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.Unauthorized);
	}

	[Fact]
	public async Task UpdateProfileAsync_SetsOnboardedAndLocation()
	{
		const string email = "update.service@example.com";
		var user = await CreateUserAsync(email);
		var principal = CreatePrincipal(user.Id);

		var dto = new UserUpdateDto(
			Nickname: "nick",
			FirstName: "First",
			LastName: "Last",
			PostalCode: "1000",
			BirthdayDate: "2001-06-01",
			SpokenLanguages: [
				new SpokenLanguageDto("FR", "Native"),
				new SpokenLanguageDto("DE", "Intermediate")
			]
		);

		var result = await _userService.UpdateProfileAsync(principal, dto);

		result.IsSuccess.Should().BeTrue();
		result.Data!.Nickname.Should().Be("nick");
		result.Data.FirstName.Should().Be("First");
		result.Data.LastName.Should().Be("Last");
		result.Data.IsOnboarded.Should().BeTrue();
		result.Data.Location!.PostalCode.Should().Be("1000");
		result.Data.SpokenLanguages.Should().HaveCount(2);

		var reloaded = await _userManager.FindByEmailAsync(email);
		reloaded!.IsOnboarded.Should().BeTrue();
		reloaded.Nickname.Should().Be("nick");
	}

	[Fact]
	public async Task UpdateProfileAsync_ReturnsBadRequest_WhenPostalCodeInvalid()
	{
		var user = await CreateUserAsync("invalid.postal@example.com");
		var principal = CreatePrincipal(user.Id);

		var dto = new UserUpdateDto(
			Nickname: "nick",
			FirstName: "A",
			LastName: "B",
			PostalCode: "0000",
			BirthdayDate: "2000-01-01",
			SpokenLanguages: [new SpokenLanguageDto("FR", "Native")]
		);

		var result = await _userService.UpdateProfileAsync(principal, dto);

		result.IsSuccess.Should().BeFalse();
		result.Errors.Should().Contain("Invalid postal code");
	}

	[Fact]
	public async Task GetPublicProfileAsync_ReturnsNickname_WhenUserIsOnboarded()
	{
		var user = await CreateUserAsync("public.service@example.com");
		var principal = CreatePrincipal(user.Id);

		await _userService.UpdateProfileAsync(principal, new UserUpdateDto(
			Nickname: "public_nick",
			FirstName: "Pub",
			LastName: "Lic",
			PostalCode: "1820",
			BirthdayDate: "1998-03-03",
			SpokenLanguages: [new SpokenLanguageDto("IT", "Native")]
		));

		var result = await _userService.GetPublicProfileAsync(user.Id);

		result.IsSuccess.Should().BeTrue();
		result.Data!.Nickname.Should().Be("public_nick");
	}

	[Fact]
	public async Task GetPublicProfileAsync_ReturnsNotFound_WhenUserNotOnboarded()
	{
		var user = await CreateUserAsync("not.onboarded@example.com");

		var result = await _userService.GetPublicProfileAsync(user.Id);

		result.IsSuccess.Should().BeFalse();
		result.ErrorType.Should().Be(ErrorType.NotFound);
	}

	private async Task<User> CreateUserAsync(string email)
	{
		var user = new User { UserName = email, Email = email };
		(await _userManager.CreateAsync(user, "Password123!")).Succeeded.Should().BeTrue();
		return user;
	}

	private static ClaimsPrincipal CreatePrincipal(string userId) =>
		new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		_provider.Dispose();
	}
}
