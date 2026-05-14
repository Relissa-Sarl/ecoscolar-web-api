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
        /// <summary>
        /// DbSet for the Advert entity, representing the adverts in the database
        /// </summary>
        public DbSet<Adverts> Adverts { get; set; } = default!;

        public DbSet<PhysicalItems> Products { get; set; }

        public DbSet<AdvertServices> Services { get; set; }

        public DbSet<Books> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PhysicalItems>().ToTable("Adverts");
            builder.Entity<AdvertServices>().ToTable("Adverts");
            builder.Entity<PhysicalItems>().ToTable("PhysicalItems");
        }
    }
}
