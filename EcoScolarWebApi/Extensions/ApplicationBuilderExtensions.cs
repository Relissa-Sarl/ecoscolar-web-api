using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Extensions;

public static class ApplicationBuilderExtensions
{
	public static void ApplyDatabaseMigrations(this IApplicationBuilder app, IConfiguration config)
	{
		if (config.GetValue<bool>("ApplyDatabaseMigrations"))
		{
			using var scope = app.ApplicationServices.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
			db.Database.Migrate();
		}
	}

	/// <summary>
	/// Seeds Swiss localities from switzerland_localities.csv when Location is empty.
	/// </summary>
	public static async Task SeedLocationsIfEmptyAsync(this WebApplication app, IConfiguration config)
	{
		if (!config.GetValue<bool>("ApplyDatabaseMigrations"))
			return;

		using var scope = app.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
		await DataSeeder.SeedLocationsIfEmptyAsync(db);
	}

	public static async Task SeedIdentityRolesAsync(this WebApplication app, IConfiguration config)
	{
		if (!config.GetValue<bool>("ApplyDatabaseMigrations"))
			return;

		using var scope = app.Services.CreateScope();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		await DataSeeder.SeedIdentityRolesAsync(roleManager);
	}

	public static async Task SeedDatabaseInDevelopmentAsync(this WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			using var scope = app.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			await DataSeeder.Seed(db, userManager, roleManager);
        }
	}
}