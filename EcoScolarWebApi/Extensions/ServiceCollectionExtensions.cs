using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Stripe;

namespace EcoScolarWebApi.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
	{
		var connectionString = config.GetConnectionString("Default")
			?? throw new InvalidOperationException("Connection string 'Default' is missing.");

		services.AddDbContext<EcoscolarDbContext>(options => options.UseSqlServer(connectionString));

		StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

		return services;
	}

	public static IServiceCollection AddAuthAndIdentity(this IServiceCollection services)
	{
		services.AddIdentityApiEndpoints<User>()
				.AddEntityFrameworkStores<EcoscolarDbContext>();

		services.ConfigureApplicationCookie(options =>
		{
			options.Cookie.Name = "Ecoscolar.Auth.Session";
			options.Cookie.HttpOnly = true;
			options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			options.Cookie.SameSite = SameSiteMode.None;
			options.ExpireTimeSpan = TimeSpan.FromDays(14);
			options.SlidingExpiration = true;
		});

		return services;
	}

	public static IServiceCollection AddSwaggerAndVersioning(this IServiceCollection services)
	{
		services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

		services.AddApiVersioning(options =>
		{
			options.DefaultApiVersion = new ApiVersion(1, 0);
			options.AssumeDefaultVersionWhenUnspecified = true;
			options.ReportApiVersions = true;
			options.ApiVersionReader = new UrlSegmentApiVersionReader();
		}).AddApiExplorer(options =>
		{
			options.GroupNameFormat = "'v'VVV";
			options.SubstituteApiVersionInUrl = true;
		});

		services.AddOpenApi();
		services.AddSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new OpenApiInfo
			{
				Version = "v1",
				Title = "EcoScolar Web API",
				Description = "API for the EcoScolar application, providing endpoints for user management, payment processing, and more."
			});

			options.TagActionsBy(apiDesc =>
			{
				var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
				return [controllerName?.ToLowerInvariant() ?? "default"];
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

		return services;
	}

	public static IServiceCollection AddEcoScolarServices(this IServiceCollection services, IConfiguration config)
	{
		var useFakeAdvertSearch = config.GetValue("Features:UseFakeAdvertSearch", defaultValue: true);

		if (useFakeAdvertSearch)
			services.AddScoped<IAdvertSearchService, FakeAdvertSearchService>();
		else
			services.AddScoped<IAdvertSearchService, AdvertSearchService>();

		services.AddScoped<IUserService, UserService>();
		services.AddTransient<IEmailSender<User>, DevEmailSenderService>();

		return services;
	}
}