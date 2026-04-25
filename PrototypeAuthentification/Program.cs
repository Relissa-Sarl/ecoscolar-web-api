using Microsoft.EntityFrameworkCore;
using PrototypeAuthentification.Data;
using PrototypeAuthentification.Models;

namespace PrototypeAuthentification
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<PrototypeAuthentificationContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("PrototypeAuthentificationContext") ?? throw new InvalidOperationException("Connection string 'PrototypeAuthentificationContext' not found.")));

            builder.Services.AddAuthorization();
            builder.Services.AddIdentityApiEndpoints<User>()
                .AddEntityFrameworkStores<PrototypeAuthentificationContext>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
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

            var app = builder.Build();

            app.UseCors("AllowFrontend");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
                app.MapOpenApi();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapIdentityApi<User>();

            app.Run();
        }
    }
}
