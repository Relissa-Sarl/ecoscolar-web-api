using DotNet.Testcontainers.Builders;
using EcoScolarWebApi.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;
using Respawn;

namespace EcoScolarWebApi.Tests;

public class CustomApiFactory : WebApplicationFactory<global::Program>, IAsyncLifetime
{
    // SQL Server container declaration
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    // Turn on the SQL container
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Open Respawn connection
        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        // ignore migration history for respawn
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
        });
    }

    // Method called for empty database before test
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Delete existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EcoscolarDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Inject new DbContext pointing on container
            services.AddDbContext<EcoscolarDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            // EF Core migration for create tables
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
            db.Database.Migrate(); // Create database schema in container
        });
    }

    // Destroy container at the end of the test
    public new async Task DisposeAsync()
    {
        await _dbConnection.CloseAsync();
        await _dbContainer.DisposeAsync();
    }
}


