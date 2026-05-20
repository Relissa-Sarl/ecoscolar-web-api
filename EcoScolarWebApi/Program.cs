using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Services;
using EcoscolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
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

            // Add services to the builder
            builder.Services.AddIdentityApiEndpoints<User>()
                .AddEntityFrameworkStores<EcoscolarDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Ecoscolar.Auth.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                // Optional: Where to redirect if an unauthenticated user tries to access a protected route
                // (Note: For APIs returning JSON, we usually return 401 instead of redirecting)
                //options.Events.OnRedirectToLogin = context =>
                //{
                //    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //    return Task.CompletedTask;
                //};
            });

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
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					In = ParameterLocation.Header,
					Description = "Please enter a valid token",
					Name = "Authorization",
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT"
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
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            // Add business logic services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddTransient<IEmailSender<User>, DevEmailSenderService>();

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
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<EcoscolarDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    // Call the seeder only when running in Development mode
                    DataSeeder.Seed(db, userManager).Wait();
                }

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
            app.MapGroup("/api/v1/auth").MapIdentityApi<User>();

            // Run the application
            app.Run();
        }
    }
}
