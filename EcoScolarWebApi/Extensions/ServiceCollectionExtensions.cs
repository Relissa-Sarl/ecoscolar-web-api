using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Minio;
using Stripe;

namespace EcoScolarWebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMappersServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<SubjectMapper>();
        services.AddSingleton<LanguageMapper>();
        services.AddSingleton<PublicCommentMapper>();
		services.AddSingleton<UserMapper>();
        services.AddSingleton<ReviewMapper>();
        services.AddSingleton<LocationMapper>();
        services.AddSingleton<AbuseReportMapper>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
	{
		if (config.GetValue("UseInMemoryDatabase", defaultValue: false))
		{
			var databaseName = config.GetValue<string>("InMemoryDatabaseName") ?? Guid.NewGuid().ToString();
			services.AddDbContext<EcoscolarDbContext>(options => options.UseInMemoryDatabase(databaseName));
		}
		else
		{
			var connectionString = config.GetConnectionString("Default")
				?? throw new InvalidOperationException("Connection string 'Default' is missing.");

			services.AddDbContext<EcoscolarDbContext>(options => options.UseSqlServer(connectionString));
		}

		StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

		return services;
	}

	public static IServiceCollection AddAuthAndIdentity(this IServiceCollection services)
	{
		services.AddIdentityApiEndpoints<User>()
				.AddRoles<IdentityRole>()
				.AddEntityFrameworkStores<EcoscolarDbContext>()
				.AddDefaultTokenProviders();

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
		services.AddTransient<IEmailSenderService, EmailSenderService>();
		services.AddTransient<IEmailSender<User>>(provider => provider.GetRequiredService<IEmailSenderService>());
		services.AddScoped<ICartService, CartService>();
		services.AddScoped<IAdminService, AdminService>();
		services.AddScoped<ISupportContactService, SupportContactService>();
		services.AddScoped<IStripeConnectService, StripeConnectService>();
		services.AddScoped<IAbuseReportService, AbuseReportService>();
		services.AddSingleton<IPlatformFeeCalculator, PlatformFeeCalculator>();
		services.AddSingleton<IShippingFeeCalculator, ShippingFeeCalculator>();
		services.AddSingleton<IStripeCheckoutClient, StripeCheckoutClient>();
		services.AddSingleton<IStripeTransferClient, StripeTransferClient>();
		services.AddSingleton<IStripeRefundClient, StripeRefundClient>();
		services.AddScoped<IPaymentService, PaymentService>();
		services.AddScoped<IPayoutService, SellerPayoutService>();
		services.AddScoped<IRefundService, PaymentRefundService>();
		services.AddScoped<ITutoringReservationService, TutoringReservationService>();
		services.AddScoped<ITutoringTransactionService, TutoringTransactionService>();
		services.AddScoped<ITutoringEscrowProcessor, TutoringEscrowProcessor>();

		// MinIO image storage
		var minioEndpoint = config["Minio:Endpoint"].NullIfEmpty() ?? "localhost:9000";
		var minioAccessKey = config["Minio:AccessKey"].NullIfEmpty() ?? "ecoscolar";
		var minioSecretKey = config["Minio:SecretKey"].NullIfEmpty() ?? "ecoscolar_secret_change_me";
		var minioUseHttps = config.GetValue("Minio:UseHttps", defaultValue: false);

		services.AddMinio(configureClient => configureClient
			.WithEndpoint(minioEndpoint)
			.WithCredentials(minioAccessKey, minioSecretKey)
			.WithSSL(minioUseHttps)
			.Build());

		services.AddScoped<IImageStorageService, MinioImageStorageService>();

        return services;
	}
}

internal static class StringExtensions
{
    internal static string? NullIfEmpty(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}