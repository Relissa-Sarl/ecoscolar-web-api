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

			builder.Entity<PhysicalItem>()
				.HasOne(p => p.ProductCategory)
				.WithMany(c => c.PhysicalItems)
				.HasForeignKey(p => p.ProductCategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			configureUserLanguageEntity(builder);
			configureLocationEntity(builder);

			Seeding(builder);
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
		/// <summary>
		/// DbSet for the Advert entity, representing the adverts in the database
		/// </summary>
		public DbSet<Advert> Adverts { get; set; } = default!;
		public DbSet<PhysicalItem> Products { get; set; } = default!;
		public DbSet<AdvertService> Services { get; set; } = default!;
		public DbSet<Book> Books { get; set; } = default!;
		public DbSet<Picture> Pictures { get; set; } = default!;
		public DbSet<ProductCategory> ProductCategories { get; set; } = default!;
		public DbSet<UserFavorite> UserFavorites { get; set; } = default!;

		private static void Seeding(ModelBuilder builder)
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
			builder.Entity<BookCategory>().HasData(
				new BookCategory { BookCategoryId = 1, Name = "Manuels scolaires", Description = "Manuels par niveau et programme cantonal." },
				new BookCategory { BookCategoryId = 2, Name = "Ouvrages de référence", Description = "Dictionnaires, encyclopédies et atlas scolaires." },
				new BookCategory { BookCategoryId = 3, Name = "Langues", Description = "Français, allemand, italien, anglais et langues locales." },
				new BookCategory { BookCategoryId = 4, Name = "Mathématiques", Description = "Supports de mathématiques pour cycles 1 à secondaire II." },
				new BookCategory { BookCategoryId = 5, Name = "Sciences naturelles", Description = "Biologie, chimie, physique et sciences de la nature." },
				new BookCategory { BookCategoryId = 6, Name = "Histoire et géographie", Description = "Histoire suisse, géographie et éducation citoyenne." },
				new BookCategory { BookCategoryId = 7, Name = "Économie et droit", Description = "Introduction à l'économie, droit et gestion." },
				new BookCategory { BookCategoryId = 8, Name = "Arts et musique", Description = "Supports pour arts visuels, musique et activités créatives." },
				new BookCategory { BookCategoryId = 9, Name = "Informatique", Description = "Initiation au numérique, algorithmique et outils informatiques." },
				new BookCategory { BookCategoryId = 10, Name = "Formation professionnelle", Description = "Manuels liés aux filières CFC et maturité professionnelle." }
			);
			builder.Entity<Subject>().HasData(
				new Subject { SubjectId = 1, Name = "Français", Code = "FR" },
				new Subject { SubjectId = 2, Name = "Allemand", Code = "DE" },
				new Subject { SubjectId = 3, Name = "Anglais", Code = "EN" },
				new Subject { SubjectId = 4, Name = "Mathématiques", Code = "MATH" },
				new Subject { SubjectId = 5, Name = "Sciences naturelles", Code = "SCI" },
				new Subject { SubjectId = 6, Name = "Histoire", Code = "HIST" },
				new Subject { SubjectId = 7, Name = "Géographie", Code = "GEO" },
				new Subject { SubjectId = 8, Name = "Éducation physique", Code = "EPS" },
				new Subject { SubjectId = 9, Name = "Arts visuels", Code = "ARTS" },
				new Subject { SubjectId = 10, Name = "Musique", Code = "MUS" },
				new Subject { SubjectId = 11, Name = "Économie et droit", Code = "ECO" },
				new Subject { SubjectId = 12, Name = "Informatique", Code = "INFO" }
			);
			// Système HarmoS : https://ecole-basse-veveyse.ch/informations/degres-harmos/
			builder.Entity<SchoolGrade>().HasData(
				new SchoolGrade { SchoolGradeId = 1, Name = "Cycle 1 (1H-4H)", Grade = "C1" },
				new SchoolGrade { SchoolGradeId = 2, Name = "Cycle 2 (5H-8H)", Grade = "C2" },
				new SchoolGrade { SchoolGradeId = 3, Name = "Cycle 3 (9H-11H)", Grade = "C3" },
				new SchoolGrade { SchoolGradeId = 4, Name = "Secondaire II - Gymnase", Grade = "S2-GYM" },
				new SchoolGrade { SchoolGradeId = 5, Name = "Secondaire II - Maturité professionnelle", Grade = "S2-MP" },
				new SchoolGrade { SchoolGradeId = 6, Name = "Secondaire II - CFC", Grade = "S2-CFC" },
				new SchoolGrade { SchoolGradeId = 7, Name = "Secondaire II - ECG", Grade = "S2-ECG" }
			);
			builder.Entity<ProductCategory>().HasData(
				new ProductCategory { ProductCategoryId = 1, Name = "Papeterie", Description = "Papiers, enveloppes, étiquettes et consommables." },
				new ProductCategory { ProductCategoryId = 2, Name = "Matériel d'écriture", Description = "Stylos, crayons, feutres et surligneurs." },
				new ProductCategory { ProductCategoryId = 3, Name = "Cahiers et classeurs", Description = "Cahiers, classeurs, intercalaires et chemises." },
				new ProductCategory { ProductCategoryId = 4, Name = "Matériel artistique", Description = "Peinture, pinceaux, papiers dessin et outils créatifs." },
				new ProductCategory { ProductCategoryId = 5, Name = "Matériel scientifique", Description = "Kits pédagogiques, microscopes et accessoires." },
				new ProductCategory { ProductCategoryId = 6, Name = "Équipement sportif", Description = "Ballons, cordes, protections et matériel EPS." },
				new ProductCategory { ProductCategoryId = 7, Name = "Matériel informatique", Description = "Claviers, souris, périphériques et accessoires." },
				new ProductCategory { ProductCategoryId = 8, Name = "Sacs et cartables", Description = "Sacs d'école, cartables et trousses." },
				new ProductCategory { ProductCategoryId = 9, Name = "Calculatrices", Description = "Calculatrices scientifiques et financières." },
				new ProductCategory { ProductCategoryId = 10, Name = "Accessoires de laboratoire", Description = "Blouses, lunettes de protection et consommables." }
			);
		}
	}
}
