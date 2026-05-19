using DotNet.Testcontainers.Builders;
using EcoscolarWebApi;
using EcoscolarWebApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

public class CustomApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 1. On déclare le conteneur SQL Server
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // 2. On démarre le conteneur avant le début des tests
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 3. On retire le DbContext existant (celui de production/développement)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EcoscolarDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // 4. On injecte un nouveau DbContext qui pointe vers le conteneur jetable
            services.AddDbContext<EcoscolarDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            // 5. On applique les migrations EF Core pour créer les tables
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
            db.Database.Migrate(); // Crée le schéma de DB dans le conteneur
        });
    }

    // 6. On détruit le conteneur à la fin des tests
    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}