using EcoScolarWebApi.Data;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoScolarWebApi.Tests.Integration;

internal static class IntegrationTestIdentityHelper
{
	internal static ServiceProvider CreateIdentityProvider(out EcoscolarDbContext context)
	{
		var databaseName = Guid.NewGuid().ToString();
		var services = new ServiceCollection();

		services.AddDbContext<EcoscolarDbContext>(options =>
			options.UseInMemoryDatabase(databaseName));

		services.AddHttpContextAccessor();
		services.AddAuthentication();
		services.AddAuthorization();

		services.AddIdentityApiEndpoints<User>()
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<EcoscolarDbContext>()
			.AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
		{
			options.Cookie.Name = "Ecoscolar.Auth.Session";
			options.Cookie.HttpOnly = true;
			options.Cookie.SecurePolicy = CookieSecurePolicy.None;
		});

        services.AddScoped<UserMapper>();
        services.AddLogging();

		var provider = services.BuildServiceProvider();
		context = provider.GetRequiredService<EcoscolarDbContext>();
		context.Database.EnsureCreated();
		SeedLocations(context);
		return provider;
	}

	internal static void SeedLocations(EcoscolarDbContext context)
	{
		try
		{
			if (context.Locations.Any())
				return;

			context.Locations.AddRange(
				new Location { LocationId = 1, PostalCode = "1000", City = "Lausanne", Region = "Vaud" },
				new Location { LocationId = 2, PostalCode = "1820", City = "Montreux", Region = "Vaud" });
			context.SaveChanges();
		}
		catch (Exception)
		{
			// Ignore concurrency/duplicate key seeding conflicts in parallel test environments
		}
	}
}
