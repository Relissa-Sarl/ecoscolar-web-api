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

            var app = builder.Build();

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
