using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Services;
using EcoscolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
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
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            // Add business logic services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddTransient<IEmailSender<User>, DevEmailSenderService>();

            // Build the application
            var app = builder.Build();

            // Use the CORS policy
            app.UseCors("AllowFrontend");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
                app.MapOpenApi();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers to the application
            app.MapControllers();
            app.MapGroup("/api/v1/auth").MapIdentityApi<User>();

            // Run the application
            app.Run();
        }
    }
}
