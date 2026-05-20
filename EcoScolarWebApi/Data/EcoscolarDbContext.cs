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
        public DbSet<EcoscolarWebApi.Models.Subjects> Subjects { get; set; } = default!;
        /// <summary>
        /// Db context constructor
        /// </summary>
        /// <param name="options">The options for the DbContext</param>
        public DbSet<EcoscolarWebApi.Models.BookCategories> BookCategories { get; set; } = default!;
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
        public DbSet<UserFavorite> UserFavorites { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserFavorite>()
                .HasOne(uf => uf.Advert)
                .WithMany()
                .HasForeignKey(uf => uf.AdvertId)
                .OnDelete(DeleteBehavior.Cascade);

            /*builder.Entity<PhysicalItems>()
                .HasOne(p => p.ProductCategory)
                .WithMany(c => c.PhysicalItems)
                .HasForeignKey(p => p.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);*/

			Seeding(builder);
        }

        private static void Seeding(ModelBuilder builder)
        {
            builder.Entity<BookCategories>().HasData(
                new BookCategories { BookCategoryId = 1, Name = "Manuels scolaires", Description = "Manuels par niveau et programme cantonal." },
                new BookCategories { BookCategoryId = 2, Name = "Ouvrages de référence", Description = "Dictionnaires, encyclopédies et atlas scolaires." },
                new BookCategories { BookCategoryId = 3, Name = "Langues", Description = "Français, allemand, italien, anglais et langues locales." },
                new BookCategories { BookCategoryId = 4, Name = "Mathématiques", Description = "Supports de mathématiques pour cycles 1 à secondaire II." },
                new BookCategories { BookCategoryId = 5, Name = "Sciences naturelles", Description = "Biologie, chimie, physique et sciences de la nature." },
                new BookCategories { BookCategoryId = 6, Name = "Histoire et géographie", Description = "Histoire suisse, géographie et éducation citoyenne." },
                new BookCategories { BookCategoryId = 7, Name = "Économie et droit", Description = "Introduction à l'économie, droit et gestion." },
                new BookCategories { BookCategoryId = 8, Name = "Arts et musique", Description = "Supports pour arts visuels, musique et activités créatives." },
                new BookCategories { BookCategoryId = 9, Name = "Informatique", Description = "Initiation au numérique, algorithmique et outils informatiques." },
                new BookCategories { BookCategoryId = 10, Name = "Formation professionnelle", Description = "Manuels liés aux filières CFC et maturité professionnelle." }
            );
            builder.Entity<Subjects>().HasData(
                new Subjects { SubjectId = 1, Name = "Français", Subject = "FR" },
                new Subjects { SubjectId = 2, Name = "Allemand", Subject = "DE" },
                new Subjects { SubjectId = 3, Name = "Anglais", Subject = "EN" },
                new Subjects { SubjectId = 4, Name = "Mathématiques", Subject = "MATH" },
                new Subjects { SubjectId = 5, Name = "Sciences naturelles", Subject = "SCI" },
                new Subjects { SubjectId = 6, Name = "Histoire", Subject = "HIST" },
                new Subjects { SubjectId = 7, Name = "Géographie", Subject = "GEO" },
                new Subjects { SubjectId = 8, Name = "Éducation physique", Subject = "EPS" },
                new Subjects { SubjectId = 9, Name = "Arts visuels", Subject = "ARTS" },
                new Subjects { SubjectId = 10, Name = "Musique", Subject = "MUS" },
                new Subjects { SubjectId = 11, Name = "Économie et droit", Subject = "ECO" },
                new Subjects { SubjectId = 12, Name = "Informatique", Subject = "INFO" }
            );
            // Système HarmoS : https://ecole-basse-veveyse.ch/informations/degres-harmos/
            builder.Entity<SchoolGrades>().HasData(
                new SchoolGrades { SchoolGradeId = 1, Name = "Cycle 1 (1H-4H)", SchoolGrade = "C1" },
                new SchoolGrades { SchoolGradeId = 2, Name = "Cycle 2 (5H-8H)", SchoolGrade = "C2" },
                new SchoolGrades { SchoolGradeId = 3, Name = "Cycle 3 (9H-11H)", SchoolGrade = "C3" },
                new SchoolGrades { SchoolGradeId = 4, Name = "Secondaire II - Gymnase", SchoolGrade = "S2-GYM" },
                new SchoolGrades { SchoolGradeId = 5, Name = "Secondaire II - Maturité professionnelle", SchoolGrade = "S2-MP" },
                new SchoolGrades { SchoolGradeId = 6, Name = "Secondaire II - CFC", SchoolGrade = "S2-CFC" },
                new SchoolGrades { SchoolGradeId = 7, Name = "Secondaire II - ECG", SchoolGrade = "S2-ECG" }
            );
            builder.Entity<ProductCategories>().HasData(
                new ProductCategories { ProductCategoryId = 1, Name = "Papeterie", Description = "Papiers, enveloppes, étiquettes et consommables." },
                new ProductCategories { ProductCategoryId = 2, Name = "Matériel d'écriture", Description = "Stylos, crayons, feutres et surligneurs." },
                new ProductCategories { ProductCategoryId = 3, Name = "Cahiers et classeurs", Description = "Cahiers, classeurs, intercalaires et chemises." },
                new ProductCategories { ProductCategoryId = 4, Name = "Matériel artistique", Description = "Peinture, pinceaux, papiers dessin et outils créatifs." },
                new ProductCategories { ProductCategoryId = 5, Name = "Matériel scientifique", Description = "Kits pédagogiques, microscopes et accessoires." },
                new ProductCategories { ProductCategoryId = 6, Name = "Équipement sportif", Description = "Ballons, cordes, protections et matériel EPS." },
                new ProductCategories { ProductCategoryId = 7, Name = "Matériel informatique", Description = "Claviers, souris, périphériques et accessoires." },
                new ProductCategories { ProductCategoryId = 8, Name = "Sacs et cartables", Description = "Sacs d'école, cartables et trousses." },
                new ProductCategories { ProductCategoryId = 9, Name = "Calculatrices", Description = "Calculatrices scientifiques et financières." },
                new ProductCategories { ProductCategoryId = 10, Name = "Accessoires de laboratoire", Description = "Blouses, lunettes de protection et consommables." }
            );
        }
    }
}
