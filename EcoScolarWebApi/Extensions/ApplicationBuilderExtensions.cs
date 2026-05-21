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

	public static async Task SeedDatabaseInDevelopmentAsync(this WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			using var scope = app.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

			await DataSeeder.Seed(db, userManager);
		}
	}
}