using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Data;

/// <summary>
/// Database context for the Ecoscolar application, inheriting from IdentityDbContext 
/// to include user management features provided by ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// Db context constructor
/// </remarks>
/// <param name="options">The options for the DbContext</param>
public class EcoscolarDbContext(DbContextOptions<EcoscolarDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Advert> Adverts { get; set; } = default!;
    public DbSet<PhysicalItem> Products { get; set; } = default!;
    public DbSet<TutoringAdvert> Services { get; set; } = default!;
    public DbSet<Book> Books { get; set; } = default!;
    public DbSet<Picture> Pictures { get; set; } = default!;
    public DbSet<ProductCategory> ProductCategories { get; set; } = default!;
    public DbSet<UserFavorite> UserFavorites { get; set; } = default!;
    public DbSet<SchoolGrade> SchoolGrades { get; set; } = default!;
    public DbSet<Subject> Subjects { get; set; } = default!;
    public DbSet<BookCategory> BookCategories { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<UserLanguage> UserLanguages { get; set; } = default!;
    public DbSet<Language> Languages { get; set; } = default!;
    public DbSet<Location> Locations { get; set; } = default!;
    public DbSet<Dispute> Disputes { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PublicComment> PublicComments { get; set; }
    public DbSet<PriceOffer> PriceOffers { get; set; }
    public DbSet<Flag> Flags { get; set; }
    public DbSet<SearchAlert> SearchAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User Favorites
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

        // Dispute
        builder.Entity<Dispute>(entity =>
        {
            entity.Property(d => d.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(d => d.Transaction).WithMany().HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.Restrict); // Save disputes even if the transaction is deleted
        });

        // Review
        builder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Transaction)
                .WithMany()
                .HasForeignKey(r => r.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction
        builder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(t => t.Advert)
                .WithMany()
                .HasForeignKey(t => t.AdvertId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Buyer)
                .WithMany()
                .HasForeignKey(t => t.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PublicComment
        builder.Entity<PublicComment>(entity =>
        {
            entity.Property(pc => pc.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(pc => pc.Advert)
                .WithMany()
                .HasForeignKey(pc => pc.AdvertId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pc => pc.Author)
                .WithMany()
                .HasForeignKey(pc => pc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PriceOffer
        builder.Entity<PriceOffer>(entity =>
        {
            entity.HasKey(po => new { po.AdvertId, po.BuyerId });

            entity.Property(po => po.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(po => po.Advert)
                .WithMany()
                .HasForeignKey(po => po.AdvertId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(po => po.Buyer)
                .WithMany()
                .HasForeignKey(po => po.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Flag
        builder.Entity<Flag>(entity =>
        {
            entity.Property(f => f.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(f => f.Reporter)
                .WithMany()
                .HasForeignKey(f => f.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Flagged)
                .WithMany()
                .HasForeignKey(f => f.FlaggedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SearchAlert
        builder.Entity<SearchAlert>(entity =>
        {
            entity.HasOne(sa => sa.User)
                .WithMany()
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sa => sa.Subject)
                .WithMany()
                .HasForeignKey(sa => sa.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sa => sa.BookCategory)
                .WithMany()
                .HasForeignKey(sa => sa.BookCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        ConfigureUserLanguageEntity(builder);
        ConfigureLocationEntity(builder);
        Seeding(builder);
    }

    private static void ConfigureUserLanguageEntity(ModelBuilder builder)
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
    private static void ConfigureLocationEntity(ModelBuilder builder)
    {
        builder.Entity<User>()
            .HasOne(u => u.Location)
            .WithMany(l => l.Users)
            .HasForeignKey(u => u.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }

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
            new SchoolGrade { SchoolGradeId = 1, Name = "Cycle 1 (1H-4H)", Code = "C1" },
            new SchoolGrade { SchoolGradeId = 2, Name = "Cycle 2 (5H-8H)", Code = "C2" },
            new SchoolGrade { SchoolGradeId = 3, Name = "Cycle 3 (9H-11H)", Code = "C3" },
            new SchoolGrade { SchoolGradeId = 4, Name = "Secondaire II - Gymnase", Code = "S2-GYM" },
            new SchoolGrade { SchoolGradeId = 5, Name = "Secondaire II - Maturité professionnelle", Code = "S2-MP" },
            new SchoolGrade { SchoolGradeId = 6, Name = "Secondaire II - CFC", Code = "S2-CFC" },
            new SchoolGrade { SchoolGradeId = 7, Name = "Secondaire II - ECG", Code = "S2-ECG" }
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
