using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// T8-1 · intégration login (Identity + EF InMemory).
/// </summary>
public class LoginIntegrationTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly EcoscolarDbContext _context;
	private readonly UserManager<User> _userManager;
	private readonly SignInManager<User> _signInManager;

	public LoginIntegrationTests()
	{
		_provider = IntegrationTestIdentityHelper.CreateIdentityProvider(out _context);
		_userManager = _provider.GetRequiredService<UserManager<User>>();
		_signInManager = _provider.GetRequiredService<SignInManager<User>>();
	}

	[Fact]
	public async Task Login_WithValidCredentials_Succeeds()
	{
		const string email = "login.integration@example.com";
		const string password = "Password123!";

		(await _userManager.CreateAsync(new User
		{
			UserName = email,
			Email = email
		}, password)).Succeeded.Should().BeTrue();

		var user = await _userManager.FindByEmailAsync(email);
		var result = await _signInManager.CheckPasswordSignInAsync(user!, password, lockoutOnFailure: false);

		result.Succeeded.Should().BeTrue();
	}

	[Fact]
	public async Task Login_WithInvalidPassword_Fails()
	{
		const string email = "login.bad@example.com";

		(await _userManager.CreateAsync(new User { UserName = email, Email = email }, "Password123!"))
			.Succeeded.Should().BeTrue();

		var user = await _userManager.FindByEmailAsync(email);
		var result = await _signInManager.CheckPasswordSignInAsync(user!, "WrongPassword!", lockoutOnFailure: false);

		result.Succeeded.Should().BeFalse();
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		_provider.Dispose();
	}
}
