using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EcoscolarWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the builder for the web application
            var builder = WebApplication.CreateBuilder(args);

            // Add db context to the builder
            var connectionString = builder.Configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Required connection string 'ConnectionStrings:Default' is missing or empty.");
            }

            builder.Services.AddDbContext<EcoscolarDbContext>(options => options.UseSqlServer(connectionString));

            // Setup Stripe
            var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = stripeSecretKey;

            // Add identity services to the builder
            builder.Services.AddIdentityApiEndpoints<User>()
                .AddEntityFrameworkStores<EcoscolarDbContext>();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // Setup CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:3000")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });

            builder.Services.AddHealthChecks();

            // Build the application
            var app = builder.Build();

            if (app.Configuration.GetValue<bool>("ApplyDatabaseMigrations"))
            {
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
                    db.Database.Migrate();  
                }
            }   

            // Use the CORS policy
            app.UseCors("AllowFrontend");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
                app.MapOpenApi();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");

            // Map controllers to the application
            app.MapControllers();
            app.MapIdentityApi<User>();

            // Run the application
            app.Run();
        }
    }
}
