using EcoScolarWebApi.Extensions;
using EcoScolarWebApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Dependency injection configuration
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthAndIdentity();
builder.Services.AddSwaggerAndVersioning();
builder.Services.AddEcoScolarServices(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(options =>
	options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy =>
	policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// App creation
var app = builder.Build();

// Migrations and seeding
app.ApplyDatabaseMigrations(app.Configuration);
await app.SeedDatabaseInDevelopmentAsync();

// Middleware configuration
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoScolar Web API V1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints mapping
app.MapHealthChecks("/health");
app.MapControllers();
app.MapGroup("/api/v1/auth").MapIdentityApi<User>();

app.Run();