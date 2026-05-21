using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
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

		services.AddIdentity<User, IdentityRole>(options =>
			{
				options.Password.RequireDigit = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireUppercase = true;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequiredLength = 8;
			})
			.AddEntityFrameworkStores<EcoscolarDbContext>()
			.AddDefaultTokenProviders();

		services.AddLogging();

		var provider = services.BuildServiceProvider();
		context = provider.GetRequiredService<EcoscolarDbContext>();
		return provider;
	}
}
