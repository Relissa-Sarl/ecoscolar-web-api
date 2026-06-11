using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Data;

public static class ProductionDataSeeder
{
    private const string DemoPrefix = "[DEMO PROD]";

    public static async Task SeedAsync(
        EcoscolarDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        bool includeDemoData,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        await DataSeeder.SeedLocationsIfEmptyAsync(context);
        await DataSeeder.SeedIdentityRolesAsync(roleManager);

        await SeedAdminsIfConfiguredAsync(context, userManager, configuration);

        if (includeDemoData)
            await SeedDemoDataAsync(context, userManager, configuration, cancellationToken);

        Console.WriteLine("[ProductionSeeder] Seed production termine.");
    }

    private static async Task SeedAdminsIfConfiguredAsync(
        EcoscolarDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        var adminConfigs = new[]
        {
            new AdminSeedConfig(
                EmailKey: "Seed:AdminEmail",
                EmailEnv: "ECOSCOLAR_SEED_ADMIN_EMAIL",
                PasswordKey: "Seed:AdminPassword",
                PasswordEnv: "ECOSCOLAR_SEED_ADMIN_PASSWORD",
                FirstNameKey: "Seed:AdminFirstName",
                FirstNameEnv: "ECOSCOLAR_SEED_ADMIN_FIRST_NAME",
                LastNameKey: "Seed:AdminLastName",
                LastNameEnv: "ECOSCOLAR_SEED_ADMIN_LAST_NAME",
                NicknameKey: "Seed:AdminNickname",
                NicknameEnv: "ECOSCOLAR_SEED_ADMIN_NICKNAME",
                DefaultNickname: "admin-1"),
            new AdminSeedConfig(
                EmailKey: "Seed:Admin2Email",
                EmailEnv: "ECOSCOLAR_SEED_ADMIN2_EMAIL",
                PasswordKey: "Seed:Admin2Password",
                PasswordEnv: "ECOSCOLAR_SEED_ADMIN2_PASSWORD",
                FirstNameKey: "Seed:Admin2FirstName",
                FirstNameEnv: "ECOSCOLAR_SEED_ADMIN2_FIRST_NAME",
                LastNameKey: "Seed:Admin2LastName",
                LastNameEnv: "ECOSCOLAR_SEED_ADMIN2_LAST_NAME",
                NicknameKey: "Seed:Admin2Nickname",
                NicknameEnv: "ECOSCOLAR_SEED_ADMIN2_NICKNAME",
                DefaultNickname: "admin-2")
        };

        var seededCount = 0;
        foreach (var adminConfig in adminConfigs)
        {
            if (await SeedAdminIfConfiguredAsync(context, userManager, configuration, adminConfig))
                seededCount++;
        }

        if (seededCount == 0)
            Console.WriteLine("[ProductionSeeder] Aucun admin seed configure (ECOSCOLAR_SEED_ADMIN_EMAIL / ADMIN2_EMAIL vides).");
    }

    private static async Task<bool> SeedAdminIfConfiguredAsync(
        EcoscolarDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration,
        AdminSeedConfig adminConfig)
    {
        var email = GetConfig(configuration, adminConfig.EmailKey, adminConfig.EmailEnv);
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is null)
        {
            var password = GetConfig(configuration, adminConfig.PasswordKey, adminConfig.PasswordEnv);
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException($"{adminConfig.PasswordEnv} est requis pour creer l'admin prod {email}.");

            var location = await GetDefaultLocationAsync(context);
            existing = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = GetConfig(configuration, adminConfig.FirstNameKey, adminConfig.FirstNameEnv) ?? "EcoScolar",
                LastName = GetConfig(configuration, adminConfig.LastNameKey, adminConfig.LastNameEnv) ?? "Admin",
                Nickname = GetConfig(configuration, adminConfig.NicknameKey, adminConfig.NicknameEnv) ?? adminConfig.DefaultNickname,
                IsOnboarded = true,
                LocationId = location?.LocationId
            };

            await EnsureIdentitySuccessAsync(userManager.CreateAsync(existing, password), "creation admin prod");
            Console.WriteLine($"[ProductionSeeder] Admin prod cree : {email}");
        }
        else
        {
            Console.WriteLine($"[ProductionSeeder] Admin prod deja existant : {email}");
        }

        await EnsureRoleAsync(userManager, existing, "Admin");
        await EnsureRoleAsync(userManager, existing, "User");
        return true;
    }

    private static async Task SeedDemoDataAsync(
        EcoscolarDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var demoPassword = GetConfig(configuration, "Seed:DemoPassword", "ECOSCOLAR_SEED_DEMO_PASSWORD");
        if (string.IsNullOrWhiteSpace(demoPassword))
            throw new InvalidOperationException("ECOSCOLAR_SEED_DEMO_PASSWORD est requis avec --include-demo-data.");

        var seller = await EnsureDemoUserAsync(
            context,
            userManager,
            "demo.seller@ecoscolar.local",
            "Vendeur",
            "Demo",
            "demo-vendeur",
            demoPassword);

        var buyer = await EnsureDemoUserAsync(
            context,
            userManager,
            "demo.buyer@ecoscolar.local",
            "Acheteur",
            "Demo",
            "demo-acheteur",
            demoPassword);

        await EnsureDemoUserAsync(
            context,
            userManager,
            "demo.student@ecoscolar.local",
            "Etudiant",
            "Demo",
            "demo-etudiant",
            demoPassword);

        await EnsureDemoUserAsync(
            context,
            userManager,
            "demo.parent@ecoscolar.local",
            "Parent",
            "Demo",
            "demo-parent",
            demoPassword);

        if (await context.Adverts.AnyAsync(a => a.Title.StartsWith(DemoPrefix), cancellationToken))
        {
            Console.WriteLine("[ProductionSeeder] Donnees demo deja presentes, aucune annonce recreee.");
            return;
        }

        var productCategoryId = await context.ProductCategories
            .OrderBy(c => c.ProductCategoryId)
            .Select(c => (long?)c.ProductCategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        var calculatorCategoryId = await context.ProductCategories
            .Where(c => c.Name == "Calculators")
            .Select(c => (long?)c.ProductCategoryId)
            .FirstOrDefaultAsync(cancellationToken) ?? productCategoryId;
        var bookCategoryId = await context.BookCategories
            .Where(c => c.Name == "Mathematics")
            .Select(c => (long?)c.BookCategoryId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await context.BookCategories.OrderBy(c => c.BookCategoryId).Select(c => (long?)c.BookCategoryId).FirstOrDefaultAsync(cancellationToken);
        var subject = await context.Subjects.OrderBy(s => s.SubjectId).FirstAsync(cancellationToken);
        var mathSubject = await context.Subjects.FirstOrDefaultAsync(s => s.Code == "MATH", cancellationToken) ?? subject;
        var grade = await context.SchoolGrades.OrderBy(g => g.SchoolGradeId).FirstAsync(cancellationToken);

        if (productCategoryId is null || bookCategoryId is null)
            throw new InvalidOperationException("Les categories de reference doivent exister avant le seed demo.");

        var now = DateTime.UtcNow;
        var calculator = new PhysicalItem
        {
            Title = $"{DemoPrefix} Calculatrice TI-30X",
            Description = "Calculatrice scientifique de demonstration pour valider le catalogue en production.",
            Price = 24.90m,
            CreatedAt = now.AddDays(-4),
            NotificationDate = now.AddDays(-3),
            Status = AdvertStatus.ACTIVE,
            SellerId = seller.Id,
            Condition = PhysicalItemCondition.LIKE_NEW,
            Weight = 0.25m,
            ProductCategoryId = calculatorCategoryId
        };

        var activeBook = new Book
        {
            Title = $"{DemoPrefix} Manuel de mathematiques",
            Description = "Manuel de mathematiques avec exercices corriges pour tester les filtres et alertes.",
            Price = 32.00m,
            CreatedAt = now.AddDays(-6),
            NotificationDate = now.AddDays(-5),
            Status = AdvertStatus.ACTIVE,
            SellerId = seller.Id,
            Condition = PhysicalItemCondition.USED,
            Weight = 0.75m,
            ProductCategoryId = productCategoryId,
            BookCategoryId = bookCategoryId.Value,
            ISBN = "978-demo-prod-01",
            Author = "Equipe EcoScolar",
            Publisher = "EcoScolar",
            Edition = "2026",
            WrittenLanguage = LanguageEnum.FR
        };

        var soldBook = new Book
        {
            Title = $"{DemoPrefix} Livre vendu pour reception",
            Description = "Annonce demo vendue pour verifier les parcours achat, expedition et reception.",
            Price = 18.50m,
            CreatedAt = now.AddDays(-12),
            NotificationDate = now.AddDays(-11),
            Status = AdvertStatus.SOLD,
            SellerId = seller.Id,
            Condition = PhysicalItemCondition.LIKE_NEW,
            Weight = 0.55m,
            ProductCategoryId = productCategoryId,
            BookCategoryId = bookCategoryId.Value,
            ISBN = "978-demo-prod-02",
            Author = "Equipe EcoScolar",
            Publisher = "EcoScolar",
            Edition = "2025",
            WrittenLanguage = LanguageEnum.FR
        };

        var tutoring = new TutoringAdvert
        {
            Title = $"{DemoPrefix} Cours de mathematiques",
            Description = "Cours de soutien demo pour verifier le catalogue des prestations.",
            Price = 45.00m,
            CreatedAt = now.AddDays(-2),
            NotificationDate = now.AddDays(-1),
            Status = AdvertStatus.ACTIVE,
            SellerId = seller.Id,
            TeachingLanguage = LanguageEnum.FR,
            StudyLevel = grade.NameFr,
            SubjectId = mathSubject.SubjectId,
            SchoolGradeId = grade.SchoolGradeId
        };

        context.Products.Add(calculator);
        context.Books.AddRange(activeBook, soldBook);
        context.Services.Add(tutoring);
        await context.SaveChangesAsync(cancellationToken);

        context.Pictures.AddRange(
            new Picture { PhysicalItemId = calculator.AdvertId, Label = "https://picsum.photos/seed/ecoscolar-prod-calculator/800/600" },
            new Picture { PhysicalItemId = activeBook.AdvertId, Label = "https://picsum.photos/seed/ecoscolar-prod-book/800/600" },
            new Picture { PhysicalItemId = soldBook.AdvertId, Label = "https://picsum.photos/seed/ecoscolar-prod-sold-book/800/600" });

        context.Transactions.Add(new Transaction
        {
            AdvertId = soldBook.AdvertId,
            BuyerId = buyer.Id,
            Date = now.AddDays(-1),
            Status = TransactionStatus.SHIPPED,
            ShippedDate = now.AddHours(-12),
            PlatformFee = 1.85m,
            BuyerConsent = true,
            SellerConsent = true
        });

        context.PublicComments.Add(new PublicComment
        {
            AdvertId = activeBook.AdvertId,
            AuthorId = buyer.Id,
            Content = "Le livre de demonstration est-il encore disponible ?",
            Answer = "Oui, il est disponible pour les tests.",
            CreatedAt = now.AddHours(-10),
            AnsweredAt = now.AddHours(-8)
        });

        await context.SaveChangesAsync(cancellationToken);
        Console.WriteLine("[ProductionSeeder] Donnees demo prod ajoutees.");
    }

    private static async Task<User> EnsureDemoUserAsync(
        EcoscolarDbContext context,
        UserManager<User> userManager,
        string email,
        string firstName,
        string lastName,
        string nickname,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            await EnsureRoleAsync(userManager, existing, "User");
            return existing;
        }

        var location = await GetDefaultLocationAsync(context);
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Nickname = nickname,
            IsOnboarded = true,
            LocationId = location?.LocationId
        };

        await EnsureIdentitySuccessAsync(userManager.CreateAsync(user, password), $"creation utilisateur demo {email}");
        await EnsureRoleAsync(userManager, user, "User");
        return user;
    }

    private static async Task<Location?> GetDefaultLocationAsync(EcoscolarDbContext context) =>
        await context.Locations
            .OrderBy(l => l.PostalCode == "1000" ? 0 : 1)
            .ThenBy(l => l.PostalCode)
            .FirstOrDefaultAsync();

    private static async Task EnsureRoleAsync(UserManager<User> userManager, User user, string role)
    {
        if (!await userManager.IsInRoleAsync(user, role))
            await EnsureIdentitySuccessAsync(userManager.AddToRoleAsync(user, role), $"ajout role {role} a {user.Email}");
    }

    private static async Task EnsureIdentitySuccessAsync(Task<IdentityResult> task, string action)
    {
        var result = await task;
        if (result.Succeeded)
            return;

        var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
        throw new InvalidOperationException($"Echec {action}: {errors}");
    }

    private static string? GetConfig(IConfiguration configuration, string key, string environmentVariable) =>
        configuration[key] ?? Environment.GetEnvironmentVariable(environmentVariable);

    private sealed record AdminSeedConfig(
        string EmailKey,
        string EmailEnv,
        string PasswordKey,
        string PasswordEnv,
        string FirstNameKey,
        string FirstNameEnv,
        string LastNameKey,
        string LastNameEnv,
        string NicknameKey,
        string NicknameEnv,
        string DefaultNickname);
}
