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
           new Language { Label = "FR", Name = "French", NameFr = "Français", NameDe = "Französisch", NameIt = "Francese" },
           new Language { Label = "DE", Name = "German", NameFr = "Allemand", NameDe = "Deutsch", NameIt = "Tedesco" },
           new Language { Label = "IT", Name = "Italian", NameFr = "Italien", NameDe = "Italien", NameIt = "Italiano" }
        );

        builder.Entity<Location>().HasData(
            new Location { LocationId = 1, PostalCode = "1000", City = "Lausanne", Region = "Vaud" },
            new Location { LocationId = 2, PostalCode = "1820", City = "Montreux", Region = "Vaud" },
            new Location { LocationId = 3, PostalCode = "1920", City = "Martigny", Region = "Valais" }
        );
        builder.Entity<BookCategory>().HasData(
            new BookCategory { BookCategoryId = 1, Name = "Textbooks", NameFr = "Manuels scolaires", NameDe = "Schulbücher", NameIt = "Libri di scuola", Description = "Manuels par niveau et programme cantonal." },
            new BookCategory { BookCategoryId = 2, Name = "Reference works", NameFr = "Ouvrages de référence", NameDe = "Referenzwerke", NameIt = "Opere di riferimento", Description = "Ouvrages de référence pour les étudiants." },
            new BookCategory { BookCategoryId = 3, Name = "Languages", NameFr = "Langues", NameDe = "Sprachen", NameIt = "Lingue", Description = "Cours et ressources pour l'apprentissage des langues." },
            new BookCategory { BookCategoryId = 4, Name = "Mathematics", NameFr = "Mathématiques", NameDe = "Mathematik", NameIt = "Matematica", Description = "Manuels et ressources pour l'enseignement des mathématiques." },
            new BookCategory { BookCategoryId = 5, Name = "Natural Sciences", NameFr = "Sciences naturelles", NameDe = "Naturwissenschaften", NameIt = "Scienze della natura", Description = "Cours et ressources pour les sciences naturelles." },
            new BookCategory { BookCategoryId = 6, Name = "History and Geography", NameFr = "Histoire et géographie", NameDe = "Geschichte und Geographie", NameIt = "Storia e geografia", Description = "Cours et ressources pour l'histoire et la géographie." },
            new BookCategory { BookCategoryId = 7, Name = "Economics and Law", NameFr = "Économie et droit", NameDe = "Wirtschaft und Recht", NameIt = "Economia e diritto", Description = "Cours et ressources pour l'économie et le droit." },
            new BookCategory { BookCategoryId = 8, Name = "Arts and Music", NameFr = "Arts et musique", NameDe = "Kunst und Musik", NameIt = "Arte e musica", Description = "Cours et ressources pour les arts et la musique." },
            new BookCategory { BookCategoryId = 9, Name = "Computer Science", NameFr = "Informatique", NameDe = "Informatik", NameIt = "Informatica", Description = "Cours et ressources pour l'informatique." },
            new BookCategory { BookCategoryId = 10, Name = "Vocational training", NameFr = "Formation professionnelle", NameDe = "Berufliche Bildung", NameIt = "Formazione professionale", Description = "Cours et ressources pour la formation professionnelle." }
        );
        builder.Entity<Subject>().HasData(
            new Subject { SubjectId = 1, Name = "French", NameFr = "Français", NameDe = "Französisch", NameIt = "Francese", Code = "FR" },
            new Subject { SubjectId = 2, Name = "German", NameFr = "Allemand", NameDe = "Deutsch", NameIt = "Tedesco", Code = "DE" },
            new Subject { SubjectId = 3, Name = "English", NameFr = "Anglais", NameDe = "Englisch", NameIt = "Inglese", Code = "EN" },
            new Subject { SubjectId = 4, Name = "Mathematics", NameFr = "Mathématiques", NameDe = "Mathematik", NameIt = "Matematica", Code = "MATH" },
            new Subject { SubjectId = 5, Name = "Natural Sciences", NameFr = "Sciences naturelles", NameDe = "Naturwissenschaften", NameIt = "Scienze della natura", Code = "SCI" },
            new Subject { SubjectId = 6, Name = "History", NameFr = "Histoire", NameDe = "Geschichte", NameIt = "Storia", Code = "HIST" },
            new Subject { SubjectId = 7, Name = "Geography", NameFr = "Géographie", NameDe = "Geographie", NameIt = "Geografia", Code = "GEO" },
            new Subject { SubjectId = 8, Name = "Physical Education", NameFr = "Éducation physique", NameDe = "Physikalische Bildung", NameIt = "Educazione fisica", Code = "EPS" },
            new Subject { SubjectId = 9, Name = "Visual Arts", NameFr = "Arts visuels", NameDe = "Visuelle Künste", NameIt = "Arti visive", Code = "ARTS" },
            new Subject { SubjectId = 10, Name = "Music", NameFr = "Musique", NameDe = "Musik", NameIt = "Musica", Code = "MUS" },
            new Subject { SubjectId = 11, Name = "Economics and Law", NameFr = "Économie et droit", NameDe = "Wirtschaft und Recht", NameIt = "Economia e diritto", Code = "ECO" },
            new Subject { SubjectId = 12, Name = "Computer Science", NameFr = "Informatique", NameDe = "Informatik", NameIt = "Informatica", Code = "INFO" }
        );
        // Système HarmoS : https://ecole-basse-veveyse.ch/informations/degres-harmos/
        builder.Entity<SchoolGrade>().HasData(
            new SchoolGrade { SchoolGradeId = 1, Name = "Cycle 1 (Grades 1–4)", NameFr = "Cycle 1 (1H-4H)", NameDe = "Stufe 1 (1H-4H)", NameIt = "Livello 1 (1H-4H)", Code = "C1" },
            new SchoolGrade { SchoolGradeId = 2, Name = "Cycle 2 (Grades 5–8)", NameFr = "Cycle 2 (5H-8H)", NameDe = "Stufe 2 (5H-8H)", NameIt = "Livello 2 (5H-8H)", Code = "C2" },
            new SchoolGrade { SchoolGradeId = 3, Name = "Cycle 3 (Grades 9–11)", NameFr = "Cycle 3 (9H-11H)", NameDe = "Stufe 3 (9H-11H)", NameIt = "Livello 3 (9H-11H)", Code = "C3" },
            new SchoolGrade { SchoolGradeId = 4, Name = "Upper Secondary School - High School", NameFr = "Secondaire II - Gymnase", NameDe = "Sekundarstufe II - Gymnasium", NameIt = "Secondaria II - Ginnasio", Code = "S2-GYM" },
            new SchoolGrade { SchoolGradeId = 5, Name = "Upper Secondary Level – Vocational Maturity", NameFr = "Secondaire II - Maturité professionnelle", NameDe = "Sekundarstufe II - Berufsmaturität", NameIt = "Secondaria II - Maturità professionale", Code = "S2-MP" },
            new SchoolGrade { SchoolGradeId = 6, Name = "Upper Secondary School – CFC", NameFr = "Secondaire II - CFC", NameDe = "Sekundarstufe II - CFC", NameIt = "Secondaria II - CFC", Code = "S2-CFC" },
            new SchoolGrade { SchoolGradeId = 7, Name = "Upper Secondary Level - ECG", NameFr = "Secondaire II - ECG", NameDe = "Sekundarstufe II - ECG", NameIt = "Secondaria II - ECG", Code = "S2-ECG" }
        );
        builder.Entity<ProductCategory>().HasData(
            new ProductCategory { ProductCategoryId = 1, Name = "Stationery", NameFr = "Papeterie", NameDe = "Papierware", NameIt = "Cartoleria", Description = "Papiers, enveloppes, étiquettes et consommables." },
            new ProductCategory { ProductCategoryId = 2, Name = "Writing supplies", NameFr = "Matériel d'écriture", NameDe = "Schreibmaterial", NameIt = "Materiale da scrittura", Description = "Stylos, crayons, feutres et surligneurs." },
            new ProductCategory { ProductCategoryId = 3, Name = "Notebooks and binders", NameFr = "Cahiers et classeurs", NameDe = "Hefte und Mappe", NameIt = "Quaderni e cartelle", Description = "Cahiers, classeurs, intercalaires et chemises." },
            new ProductCategory { ProductCategoryId = 4, Name = "Art supplies", NameFr = "Matériel artistique", NameDe = "Künstlerisches Material", NameIt = "Materiale artistico", Description = "Peinture, pinceaux, papiers dessin et outils créatifs." },
            new ProductCategory { ProductCategoryId = 5, Name = "Scientific equipment", NameFr = "Matériel scientifique", NameDe = "Wissenschaftliches Material", NameIt = "Materiale scientifico", Description = "Kits pédagogiques, microscopes et accessoires." },
            new ProductCategory { ProductCategoryId = 6, Name = "Sports equipment", NameFr = "Équipement sportif", NameDe = "Sportausrüstung", NameIt = "Equipaggiamento sportivo", Description = "Ballons, cordes, protections et matériel EPS." },
            new ProductCategory { ProductCategoryId = 7, Name = "Computer equipment", NameFr = "Matériel informatique", NameDe = "Informatikmaterial", NameIt = "Materiale informatico", Description = "Claviers, souris, périphériques et accessoires." },
            new ProductCategory { ProductCategoryId = 8, Name = "School bags and cases", NameFr = "Sacs et cartables", NameDe = "Schulrucksäcke und Bücherfächer", NameIt = "Ziole e borse scolastiche", Description = "Sacs d'école, cartables et trousses." },
            new ProductCategory { ProductCategoryId = 9, Name = "Calculators", NameFr = "Calculatrices", NameDe = "Taschenrechner", NameIt = "Calcolatrici", Description = "Calculatrices scientifiques et financières." },
            new ProductCategory { ProductCategoryId = 10, Name = "Laboratory supplies", NameFr = "Accessoires de laboratoire", NameDe = "Laboratoriumsausrüstung", NameIt = "Accessori del laboratorio", Description = "Blouses, lunettes de protection et consommables." }
        );
    }
}
