using EcoScolarWebApi.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// WebApplicationFactory avec EF InMemory pour tests HTTP auth (cookies + bearer).
/// </summary>
public class AuthInMemoryWebApplicationFactory : WebApplicationFactory<global::Program>
{
	private readonly string _keysPath = Path.Combine(Path.GetTempPath(), "ecoscolar-auth-tests", Guid.NewGuid().ToString("N"));
	private bool _seeded;

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureLogging(logging => logging.ClearProviders());

		builder.ConfigureTestServices(services =>
		{
			Directory.CreateDirectory(_keysPath);

			services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(_keysPath))
				.SetApplicationName("EcoScolarAuthTests");

			services.Configure<IdentityOptions>(options =>
			{
				options.SignIn.RequireConfirmedAccount = false;
				options.User.RequireUniqueEmail = true;
			});

			services.ConfigureApplicationCookie(options =>
			{
				options.Cookie.Name = "Ecoscolar.Auth.Session";
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.None;
				options.Cookie.SameSite = SameSiteMode.Lax;
			});
		});
	}

	public void EnsureSeeded()
	{
		if (_seeded)
			return;

		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
		db.Database.EnsureCreated();
		IntegrationTestIdentityHelper.SeedLocations(db);
		_seeded = true;
	}

	public HttpClient CreateCookieClient()
	{
		EnsureSeeded();
		return CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && Directory.Exists(_keysPath))
		{
			try { Directory.Delete(_keysPath, recursive: true); } catch { /* best effort cleanup */ }
		}

		base.Dispose(disposing);
	}
}
