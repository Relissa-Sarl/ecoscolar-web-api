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

        public DbSet<PhysicalItems> Products { get; set; } = default!;

        public DbSet<AdvertServices> Services { get; set; } = default!;

        public DbSet<Books> Books { get; set; } = default!;

        public DbSet<Pictures> Pictures { get; set; } = default!;
        public DbSet<ProductCategories> ProductCategories { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<PhysicalItems>()
                .HasOne(p => p.ProductCategory)
                .WithMany(c => c.PhysicalItems)
                .HasForeignKey(p => p.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            this.seeding(builder);
        }

        private void seeding(ModelBuilder builder)
        {
            builder.Entity<BookCategories>().HasData(
                new BookCategories { BookCategoryId = 1, Description = "description", Name = "first cat" }
            );
            builder.Entity<Subjects>().HasData(
                new Subjects { SubjectId = 1, Name = "Math", Subject = "MATH"}
            );
            builder.Entity<SchoolGrades>().HasData(
                new SchoolGrades { SchoolGradeId = 1, Name = "École Supérieur", SchoolGrade = "ES"}
            );
            builder.Entity<ProductCategories>().HasData(
                new ProductCategories { ProductCategoryId = 1, Name = "Fournitures", Description = "Catégorie exemple pour produits non-livres" }
            );
        }
    }
}
