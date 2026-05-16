using EcoscolarWebApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Data
{
    /// <summary>
    /// Database context for the Ecoscolar application, inheriting from IdentityDbContext 
    /// to include user management features provided by ASP.NET Core Identity.
    /// </summary>
    public class EcoscolarDbContext : IdentityDbContext<User>
    {
        /// <summary>
        /// Db context constructor
        /// </summary>
        /// <param name="options">The options for the DbContext</param>
        public EcoscolarDbContext(DbContextOptions<EcoscolarDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// DbSet for the User entity, representing the users in the database
        /// </summary>
        public DbSet<User> User { get; set; } = default!;

        public DbSet<UserLanguage> UserLanguages { get; set; } = default!;

        public DbSet<Language> Languages { get; set; } = default!;

        public DbSet<Location> Locations { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Define the composite primary key for the UserLanguage entity and configure the relationships
            base.OnModelCreating(builder);

            this.configureUserLanguageEntity(builder);
            this.configureLocationEntity(builder);

            this.seeding(builder);
        }

        private void configureUserLanguageEntity(ModelBuilder builder)
        { 
            builder.Entity<UserLanguage>()
                .HasKey(ul => new { ul.UserId, ul.Label });

            builder.Entity<UserLanguage>()
                .HasOne(ul => ul.User)
                .WithMany(u => u.Languages)
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserLanguage>()
                .HasOne(ul => ul.Language)
                .WithMany(l => l.UserLanguages)
                .HasForeignKey(ul => ul.Label)
                .OnDelete(DeleteBehavior.Cascade);
        }
        private void configureLocationEntity(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany(l => l.Users)
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void seeding(ModelBuilder builder)
        {
            builder.Entity<Language>().HasData(
                new Language { Label = "FR", Name = "Français" },
                new Language { Label = "DE", Name = "Deutsch" },
                new Language { Label = "IT", Name = "Italian" }
            );

            builder.Entity<Location>().HasData(
                new Location { LocationId = 1, PostalCode = "1000", City = "Lausanne", Region = "Vaud" },
                new Location { LocationId = 2, PostalCode = "1820", City = "Montreux", Region = "Vaud" },
                new Location { LocationId = 3, PostalCode = "1920", City = "Martigny", Region = "Valais" }
            );
        }
    }
}
