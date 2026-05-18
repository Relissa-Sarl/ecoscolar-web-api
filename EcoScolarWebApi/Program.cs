using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

            var useFakeAdvertSearch = builder.Configuration.GetValue("Features:UseFakeAdvertSearch", defaultValue: true);
            if (useFakeAdvertSearch)
            {
                builder.Services.AddScoped<IAdvertSearchService, FakeAdvertSearchService>();
            }
            else
            {
                builder.Services.AddScoped<IAdvertSearchService, AdvertSearchService>();
            }

            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "EcoScolar Web API",
                    Description = "API for the EcoScolar application, providing endpoints for user management, payment processing, and more.",
                    Contact = new OpenApiContact
                    {
                        Name = "EcoScolar Support",
                        Email = "email@here.com"
                    }
                });
            });

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
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoScolar Web API V1");
                });
            }

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
