using DotNet.Testcontainers.Builders;
using System.Globalization;
using Bogus;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Data;

public class DataSeeder
{
	public static async Task Seed(EcoscolarDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
	{
		// 1. Import Swiss localities from CSV (postal code + city + region)
		await SeedLocationsIfEmptyAsync(context);

		await SeedIdentityRolesAsync(roleManager);

		if (await context.Users.AnyAsync())
			return;

    Randomizer.Seed = new Random(2025);
		var faker = new Faker("fr_CH");
		var users = new List<User>();
    
		// 2. Generate data to test (Albert / Marie)
		await SeedTestDataAsync(context, userManager);

		// 3. Generate random data for more realistic testing
		await SeedRandomDataAsync(context, userManager);
	}

	public static async Task SeedIdentityRolesAsync(RoleManager<IdentityRole> roleManager)
	{
		if (!await roleManager.RoleExistsAsync("User"))
			await roleManager.CreateAsync(new IdentityRole("User"));

		if (!await roleManager.RoleExistsAsync("Admin"))
			await roleManager.CreateAsync(new IdentityRole("Admin"));
	}

	/// <summary>
	/// Imports Swiss localities from CSV when the Location table is empty.
	/// </summary>
	public static async Task SeedLocationsIfEmptyAsync(EcoscolarDbContext context)
	{
		if (await context.Locations.AnyAsync())
			return;

		var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", "switzerland_localities.csv");

		if (!File.Exists(filePath))
		{
			Console.WriteLine($"[Seeder] Fichier CSV introuvable au chemin : {filePath}");
			return;
		}

		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			MissingFieldFound = null,
			BadDataFound = null
		};

		using var reader = new StreamReader(filePath);
		using var csv = new CsvReader(reader, config);

		var records = csv.GetRecords<SwissCsvModel>().ToList();

		// GroupBy to merge duplicates (same postal code + city) and take the first region found for that group
		var locationsToInsert = records
			.GroupBy(r => new { r.PostalCode, r.City })
			.Select(g => new Location
			{
				PostalCode = g.Key.PostalCode.ToString(),
				City = g.Key.City,
				Region = g.First().Region
			})
			.ToList();

		await context.Locations.AddRangeAsync(locationsToInsert);
		await context.SaveChangesAsync();

		Console.WriteLine($"[Seeder] Importation réussie : {locationsToInsert.Count} localités suisses ajoutées.");
	}

	private class SwissCsvModel
	{
		[Name("PostalCode")]
		public int PostalCode { get; set; }

		[Name("City")]
		public string City { get; set; }

		[Name("Region")]
		public string Region { get; set; }
	}

	private static async Task SeedTestDataAsync(EcoscolarDbContext context, UserManager<User> userManager)
	{
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

        await userManager.AddToRoleAsync(albert, "Admin");

        // Get category IDs for test products
        var productCategoryId = await context.Set<ProductCategory>().Select(p => p.ProductCategoryId).FirstOrDefaultAsync();
		var bookCategoryId = await context.Set<BookCategory>().Select(b => b.BookCategoryId).FirstOrDefaultAsync();

		var albertItemSold = new PhysicalItem
		{
			Title = "Microscope d'Albert",
			Description = "Article vendu par Albert, acheté par Marie.",
			Price = 150m,
			CreatedAt = DateTime.UtcNow.AddDays(-10),
			Status = AdvertStatus.SOLD,
			SellerId = albert.Id,
			Condition = PhysicalItemCondition.LIKE_NEW,
			ProductCategoryId = productCategoryId
		};

		var albertItemActive = new PhysicalItem
		{
			Title = "Sac à dos d'Albert",
			Description = "Article actuellement en vente par Albert.",
			Price = 25m,
			CreatedAt = DateTime.UtcNow.AddDays(-2),
			Status = AdvertStatus.ACTIVE,
			SellerId = albert.Id,
			Condition = PhysicalItemCondition.LIKE_NEW,
			ProductCategoryId = productCategoryId
		};

		var albertBookSold = new Book
		{
			Title = "Physique Quantique pour les nuls",
			Description = "Livre vendu par Albert, acheté par Marie.",
			Price = 40m,
			CreatedAt = DateTime.UtcNow.AddDays(-15),
			Status = AdvertStatus.SOLD,
			SellerId = albert.Id,
			Condition = PhysicalItemCondition.NEW,
			BookCategoryId = bookCategoryId,
			ISBN = "9781234567890",
			Author = "Albert E.",
			Publisher = "ScienceEd",
			Edition = "2020",
			WrittenLanguage = LanguageEnum.FR
		};

		var marieItemSold = new PhysicalItem
		{
			Title = "Bécher de Marie",
			Description = "Article vendu par Marie, acheté par Albert.",
			Price = 45m,
			CreatedAt = DateTime.UtcNow.AddDays(-8),
			Status = AdvertStatus.SOLD,
			SellerId = marie.Id,
			Condition = PhysicalItemCondition.NEW,
			ProductCategoryId = productCategoryId
		};

		context.Products.AddRange(albertItemSold, albertItemActive, marieItemSold);
		context.Books.Add(albertBookSold);
		await context.SaveChangesAsync();

		var transactions = new List<Transaction>
		{
			new()
			{
				AdvertId = albertItemSold.AdvertId,
				BuyerId = marie.Id,
				Date = DateTime.UtcNow.AddDays(-5),
				Status = TransactionStatus.COMPLETED,
				PlatformFee = 2.50m,
				BuyerConsent = true,
				SellerConsent = true
			},
			new()
			{
				AdvertId = albertBookSold.AdvertId,
				BuyerId = marie.Id,
				Date = DateTime.UtcNow.AddDays(-3),
				Status = TransactionStatus.COMPLETED,
				PlatformFee = 1.50m,
				BuyerConsent = true,
				SellerConsent = true
			},
			new()
			{
				AdvertId = marieItemSold.AdvertId,
				BuyerId = albert.Id,
				Date = DateTime.UtcNow.AddDays(-2),
				Status = TransactionStatus.COMPLETED,
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
		var faker = new Faker("fr_CH");
		var randomUsers = new List<User>();

		for (var i = 1; i <= 20; i++)
		{
			var firstName = faker.Name.FirstName();
			var lastName = faker.Name.LastName();
			var email = faker.Internet.Email(firstName, lastName, "example.ch");

			var user = new User
			{
				Id = Guid.NewGuid().ToString(),
				Nickname = faker.Internet.UserName(firstName, lastName),
				UserName = email,
				Email = email,
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

		// Refresh the users list from the database to ensure all identities are persisted
		var usersInDb = await context.Users.ToListAsync();
		var userIds = usersInDb.Select(u => u.Id).ToList();
        usersInDb = context.Users.Where(u => userIds.Contains(u.Id)).ToList();

		var bookCategoryIds = await context.Set<BookCategory>().AsNoTracking().Select(c => c.BookCategoryId).ToListAsync();
		var subjectList = await context.Set<Subject>().AsNoTracking().ToListAsync();
		var schoolGradeList = await context.Set<SchoolGrade>().AsNoTracking().ToListAsync();
		var productCategoryIds = await context.Set<ProductCategory>().AsNoTracking().Select(c => c.ProductCategoryId).ToListAsync();

		if (!bookCategoryIds.Any() || !subjectList.Any() || !schoolGradeList.Any() || !productCategoryIds.Any())
			return;

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
            .RuleFor(s => s.Status, f => f.PickRandomWithout(AdvertStatus.SOLD, AdvertStatus.PAUSED))
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

        var publicComments = new List<PublicComment>
        {
            new()
            {
                AdvertId = physicalItems[0].AdvertId,
                AuthorId = usersInDb[1].Id,
                Content = "Peut-on récupérer l'objet rapidement ?",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                AdvertId = books.First().AdvertId,
                AuthorId = usersInDb[2].Id,
                Content = "Le manuel est-il encore en bon état ?",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                AdvertId = physicalItems[0].AdvertId,
                AuthorId = usersInDb[3].Id,
                Content = "J'ai une question sur cet article de test.",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        context.PublicComments.AddRange(publicComments);
        await context.SaveChangesAsync();

        foreach (var user in usersInDb)
            await userManager.AddToRoleAsync(user, "User");

        await context.SaveChangesAsync();
    }
}
