using Bogus;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Data;

public class DataSeeder
{
	public static async Task Seed(EcoscolarDbContext context, UserManager<User> userManager)
	{
		if (context.Users.Any())
		{
			return; // DB has already been seeded
		}

		Randomizer.Seed = new Random(2025);
		var faker = new Faker("fr_CH");
		var users = new List<User>();

		// Create Seller to test
		var testUser = new User
		{
			Id = Guid.NewGuid().ToString(),
			UserName = "albert@einstein.ch",
			Email = "albert@einstein.ch",
			EmailConfirmed = true,
			FirstName = "Albert",
			LastName = "Epstein"
		};
		users.Add(testUser);

		for (var i = 1; i <= 20; i++)
		{
			var firstName = faker.Name.FirstName();
			var lastName = faker.Name.LastName();
			var userName = $"{firstName}.{lastName}".ToLowerInvariant().Replace(" ", string.Empty) + $"{i}@example.ch";
			var email = userName;
			var user = new User
			{
				Id = Guid.NewGuid().ToString(),
				UserName = userName,
				Email = email,
				EmailConfirmed = true,
				FirstName = firstName,
				LastName = lastName
			};
			users.Add(user);
		}

		foreach (var user in users)
		{
			await userManager.CreateAsync(user, "P@ssw0rd!");
		}

		// Refresh the users list from the database to ensure all identities are persisted
		var userIds = users.Select(u => u.Id).ToList();
		users = context.Users.Where(u => userIds.Contains(u.Id)).ToList();

        // Re-assign testUser with the one saved in db to have its proper relations
        testUser = users.First(u => u.Email == "albert@einstein.ch");

		var bookCategories = context.Set<BookCategory>().AsNoTracking().ToList();
		var subjects = context.Set<Subject>().AsNoTracking().ToList();
		var schoolGrades = context.Set<SchoolGrade>().AsNoTracking().ToList();
		var productCategories = context.Set<ProductCategory>().AsNoTracking().ToList();

		if (!bookCategories.Any() || !subjects.Any() || !schoolGrades.Any() || !productCategories.Any())
		{
			return;
		}

		var bookCategoryIds = bookCategories.Select(category => category.BookCategoryId).ToList();
		var subjectList = subjects.ToList();
		var schoolGradeList = schoolGrades.ToList();
		var productCategoryIds = productCategories.Select(category => category.ProductCategoryId).ToList();

		var physicalItemsFaker = new Faker<PhysicalItem>("fr_CH")
			.RuleFor(p => p.Title, f => f.Commerce.ProductName())
			.RuleFor(p => p.Description, f => f.Lorem.Paragraphs(2))
			.RuleFor(p => p.Price, f => decimal.Round(f.Random.Decimal(5m, 250m), 2))
			.RuleFor(p => p.CreatedAt, f => f.Date.Recent(90, DateTime.UtcNow))
			.RuleFor(p => p.NotificationDate, (f, p) => p.CreatedAt.AddDays(f.Random.Int(1, 30)))
			.RuleFor(p => p.Status, f => f.PickRandom<AdvertStatus>())
			.RuleFor(p => p.SellerId, f => f.PickRandom(users).Id)
			.RuleFor(p => p.Condition, f => f.PickRandom<PhysicalItemCondition>())
			.RuleFor(p => p.Weight, f => f.Random.Bool(0.7f) ? decimal.Round(f.Random.Decimal(0.2m, 5m), 2) : null)
			.RuleFor(p => p.ProductCategoryId, f => f.Random.Bool(0.8f) ? f.Random.ListItem(productCategoryIds) : null);

		var physicalItems = physicalItemsFaker.Generate(25);

		var booksFaker = new Faker<Book>("fr_CH")
			.RuleFor(b => b.Title, f => $"Manuel de {f.Commerce.Department()}")
			.RuleFor(b => b.Description, f => f.Lorem.Paragraphs(2))
			.RuleFor(b => b.Price, f => decimal.Round(f.Random.Decimal(8m, 120m), 2))
			.RuleFor(b => b.CreatedAt, f => f.Date.Recent(180, DateTime.UtcNow))
			.RuleFor(b => b.NotificationDate, (f, b) => b.CreatedAt.AddDays(f.Random.Int(5, 45)))
			.RuleFor(b => b.Status, f => f.PickRandom<AdvertStatus>())
			.RuleFor(b => b.SellerId, f => f.PickRandom(users).Id)
			.RuleFor(b => b.Condition, f => f.PickRandom<PhysicalItemCondition>())
			.RuleFor(b => b.Weight, f => decimal.Round(f.Random.Decimal(0.3m, 2.5m), 2))
			.RuleFor(b => b.ISBN, f => $"978{f.Random.Long(1000000000L, 9999999999L)}")
			.RuleFor(b => b.Author, f => f.Name.FullName())
			.RuleFor(b => b.Publisher, f => f.Company.CompanyName())
			.RuleFor(b => b.Edition, f => $"{f.Random.Int(2019, 2025)}")
			.RuleFor(b => b.WrittenLanguage, f => f.PickRandom<Enums.LanguageEnum>())
			.RuleFor(b => b.BookCategoryId, f => f.Random.ListItem(bookCategoryIds))
			.RuleFor(b => b.ProductCategoryId, f => f.Random.Bool(0.3f) ? f.Random.ListItem(productCategoryIds) : null);

		var books = booksFaker.Generate(15);

		var servicesFaker = new Faker<TutoringAdvert>("fr_CH")
			.RuleFor(s => s.Title, f => $"Cours de {f.Random.ListItem(subjectList).Name}")
			.RuleFor(s => s.Description, f => f.Lorem.Paragraphs(2))
			.RuleFor(s => s.Price, f => decimal.Round(f.Random.Decimal(20m, 90m), 2))
			.RuleFor(s => s.CreatedAt, f => f.Date.Recent(60, DateTime.UtcNow))
			.RuleFor(s => s.NotificationDate, (f, s) => s.CreatedAt.AddDays(f.Random.Int(3, 20)))
			.RuleFor(s => s.Status, f => f.PickRandom<AdvertStatus>())
			.RuleFor(s => s.SellerId, f => f.PickRandom(users).Id)
			.RuleFor(s => s.TeachingLanguage, f => f.PickRandom<Enums.LanguageEnum>())
			.RuleFor(s => s.StudyLevel, f => f.Random.ListItem(schoolGradeList).Name)
			.RuleFor(s => s.SubjectId, f => f.Random.ListItem(subjectList).SubjectId)
			.RuleFor(s => s.SchoolGradeId, f => f.Random.ListItem(schoolGradeList).SchoolGradeId);

		var services = servicesFaker.Generate(18);

        // Force some adverts to be sold/owned by albert for testing purposes
        // 1. Give Albert some sales
        physicalItems[0].SellerId = testUser.Id;
        physicalItems[0].Status = AdvertStatus.SOLD;
        physicalItems[1].SellerId = testUser.Id;
        physicalItems[1].Status = AdvertStatus.ACTIVE;
        books[0].SellerId = testUser.Id;
        books[0].Status = AdvertStatus.SOLD;

		context.Products.AddRange(physicalItems);
		context.Books.AddRange(books);
		context.Services.AddRange(services);
		context.SaveChanges();

        // Génération des données de test spécifiques (pour tes tests manuels)
        await SeedTestDataAsync(context, userManager);

        // Génération des données aléatoires (pour populer la DB)
        await SeedRandomDataAsync(context, userManager);
    }

    private static async Task SeedTestDataAsync(EcoscolarDbContext context, UserManager<User> userManager)
    {
        // --- UTILISATEURS DE TEST ---
        var albert = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "albert@einstein.ch",
            Nickname = "nick-albertoeins",
            Email = "albert@einstein.ch",
            EmailConfirmed = true,
            FirstName = "Albert",
            LastName = "Einstein"
        };

        var marie = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "marie@curie.ch",
            Nickname = "nick-mariecurie",
            Email = "marie@curie.ch",
            EmailConfirmed = true,
            FirstName = "Marie",
            LastName = "Curie"
        };

        await userManager.CreateAsync(albert, "P@ssw0rd!");
        await userManager.CreateAsync(marie, "P@ssw0rd!");

        // --- CATÉGORIES (Récupération de la première dispo pour les tests) ---
        var productCategoryId = await context.Set<ProductCategory>().Select(p => p.ProductCategoryId).FirstOrDefaultAsync();

        // --- ARTICLES DE TEST ---
        var albertItem = new PhysicalItem // ID 1
        {
            Title = "Microscope d'Albert (Test)",
            Description = "Article vendu par Albert, acheté par Marie.",
            Price = 150m,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Status = AdvertStatus.SOLD,
            SellerId = albert.Id,
            Condition = PhysicalItemCondition.LIKE_NEW,
            ProductCategoryId = productCategoryId
        };

        var marieItem = new PhysicalItem // ID 2
        {
            Title = "Bécher de Marie (Test)",
            Description = "Article vendu par Marie, acheté par Albert.",
            Price = 45m,
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            Status = AdvertStatus.SOLD,
            SellerId = marie.Id,
            Condition = PhysicalItemCondition.NEW,
            ProductCategoryId = productCategoryId
        };

        context.Products.AddRange(albertItem, marieItem);
        await context.SaveChangesAsync();

        // --- TRANSACTIONS DE TEST ---
        var transactions = new List<Transaction>
        {
            // Marie achète l'article d'Albert
            new() // ID 1
            {
                AdvertId = albertItem.AdvertId,
                BuyerId = marie.Id,
                Date = DateTime.UtcNow.AddDays(-5),
                Status = "COMPLETED",
                PlatformFee = 2.50m,
                BuyerConsent = true,
                SellerConsent = true
            },
            // Albert achète l'article de Marie
            new() // ID 2
            {
                AdvertId = marieItem.AdvertId,
                BuyerId = albert.Id,
                Date = DateTime.UtcNow.AddDays(-2),
                Status = "COMPLETED",
                PlatformFee = 2.50m,
                BuyerConsent = true,
                SellerConsent = true
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRandomDataAsync(EcoscolarDbContext context, UserManager<User> userManager)
    {
        Randomizer.Seed = new Random(2025);
        var faker = new Faker();
        var randomUsers = new List<User>();

        // --- UTILISATEURS ALÉATOIRES ---
        for (var i = 1; i <= 20; i++)
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            var userName = $"{firstName}.{lastName}".ToLowerInvariant().Replace(" ", string.Empty) + $"{i}@example.ch";

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Nickname = $"nick-{userName}",
                UserName = userName,
                Email = userName,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };
            randomUsers.Add(user);
        }

        foreach (var user in randomUsers)
        {
            await userManager.CreateAsync(user, "P@ssw0rd!");
        }
        var usersInDb = await context.Users.ToListAsync();
        // --- RÉCUPÉRATION DES CATÉGORIES ---
        var bookCategoryIds = await context.Set<BookCategory>().AsNoTracking().Select(c => c.BookCategoryId).ToListAsync();
        var subjectList = await context.Set<Subject>().AsNoTracking().ToListAsync();
        var schoolGradeList = await context.Set<SchoolGrade>().AsNoTracking().ToListAsync();
        var productCategoryIds = await context.Set<ProductCategory>().AsNoTracking().Select(c => c.ProductCategoryId).ToListAsync();

        if (!bookCategoryIds.Any() || !subjectList.Any() || !schoolGradeList.Any() || !productCategoryIds.Any())
            return;

        // --- GÉNÉRATION BOGUS ---
        var physicalItemsFaker = new Faker<PhysicalItem>("fr_CH")
            .RuleFor(p => p.Title, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Lorem.Paragraphs(2))
            .RuleFor(p => p.Price, f => decimal.Round(f.Random.Decimal(5m, 250m), 2))
            .RuleFor(p => p.CreatedAt, f => f.Date.Recent(90, DateTime.UtcNow))
            .RuleFor(p => p.NotificationDate, (f, p) => p.CreatedAt.AddDays(f.Random.Int(1, 30)))
            .RuleFor(p => p.Status, f => f.PickRandom<AdvertStatus>())
            .RuleFor(p => p.SellerId, f => f.PickRandom(usersInDb).Id)
            .RuleFor(p => p.Condition, f => f.PickRandom<PhysicalItemCondition>())
            .RuleFor(p => p.Weight, f => f.Random.Bool(0.7f) ? decimal.Round(f.Random.Decimal(0.2m, 5m), 2) : null)
            .RuleFor(p => p.ProductCategoryId, f => f.Random.Bool(0.8f) ? f.Random.ListItem(productCategoryIds) : null);

        var physicalItems = physicalItemsFaker.Generate(25);

        var booksFaker = new Faker<Book>("fr_CH")
            .RuleFor(b => b.Title, f => $"Manuel de {f.Commerce.Department()}")
            .RuleFor(b => b.Description, f => f.Lorem.Paragraphs(2))
            .RuleFor(b => b.Price, f => decimal.Round(f.Random.Decimal(8m, 120m), 2))
            .RuleFor(b => b.CreatedAt, f => f.Date.Recent(180, DateTime.UtcNow))
            .RuleFor(b => b.NotificationDate, (f, b) => b.CreatedAt.AddDays(f.Random.Int(5, 45)))
            .RuleFor(b => b.Status, f => f.PickRandom<AdvertStatus>())
            .RuleFor(b => b.SellerId, f => f.PickRandom(usersInDb).Id)
            .RuleFor(b => b.Condition, f => f.PickRandom<PhysicalItemCondition>())
            .RuleFor(b => b.Weight, f => decimal.Round(f.Random.Decimal(0.3m, 2.5m), 2))
            .RuleFor(b => b.ISBN, f => $"978{f.Random.Long(1000000000L, 9999999999L)}")
            .RuleFor(b => b.Author, f => f.Name.FullName())
            .RuleFor(b => b.Publisher, f => f.Company.CompanyName())
            .RuleFor(b => b.Edition, f => $"{f.Random.Int(2019, 2025)}")
            .RuleFor(b => b.WrittenLanguage, f => f.PickRandom<LanguageEnum>())
            .RuleFor(b => b.BookCategoryId, f => f.Random.ListItem(bookCategoryIds))
            .RuleFor(b => b.ProductCategoryId, f => f.Random.Bool(0.3f) ? f.Random.ListItem(productCategoryIds) : null);

        var books = booksFaker.Generate(15);

        var servicesFaker = new Faker<TutoringAdvert>("fr_CH")
            .RuleFor(s => s.Title, f => $"Cours de {f.Random.ListItem(subjectList).Name}")
            .RuleFor(s => s.Description, f => f.Lorem.Paragraphs(2))
            .RuleFor(s => s.Price, f => decimal.Round(f.Random.Decimal(20m, 90m), 2))
            .RuleFor(s => s.CreatedAt, f => f.Date.Recent(60, DateTime.UtcNow))
            .RuleFor(s => s.NotificationDate, (f, s) => s.CreatedAt.AddDays(f.Random.Int(3, 20)))
            .RuleFor(s => s.Status, f => f.PickRandom<AdvertStatus>())
            .RuleFor(s => s.SellerId, f => f.PickRandom(usersInDb).Id)
            .RuleFor(s => s.TeachingLanguage, f => f.PickRandom<LanguageEnum>())
            .RuleFor(s => s.StudyLevel, f => f.Random.ListItem(schoolGradeList).Name)
            .RuleFor(s => s.SubjectId, f => f.Random.ListItem(subjectList).SubjectId)
            .RuleFor(s => s.SchoolGradeId, f => f.Random.ListItem(schoolGradeList).SchoolGradeId);

        var services = servicesFaker.Generate(18);

        context.Products.AddRange(physicalItems);
        context.Books.AddRange(books);
        context.Services.AddRange(services);
        await context.SaveChangesAsync();

        // --- IMAGES ET COMMENTAIRES ---
        var pictures = new List<Picture>();
        foreach (var item in physicalItems.Cast<PhysicalItem>().Concat(books))
        {
            var count = faker.Random.Int(1, 3);
            for (var i = 1; i <= count; i++)
            {
                pictures.Add(new Picture
                {
                    Label = $"https://picsum.photos/seed/{item.AdvertId}-{i}/800/600",
                    PhysicalItemId = item.AdvertId
                });
            }
        }
        context.Pictures.AddRange(pictures);
        await context.SaveChangesAsync();
    }
}