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
		if (context.User.Any())
		{
			return; // DB has already been seeded
		}

		Randomizer.Seed = new Random(2025);
		var faker = new Faker("fr_CH");
		var users = new List<User>();

		// Create User to test
		var testUser = new User
		{
			Id = Guid.NewGuid().ToString(),
			UserName = "albert@einstein.ch", // Utilisé pour le /login
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
			.RuleFor(p => p.UserId, f => f.PickRandom(users).Id)
			.RuleFor(p => p.Condition, f => f.PickRandom<Condition>())
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
			.RuleFor(b => b.UserId, f => f.PickRandom(users).Id)
			.RuleFor(b => b.Condition, f => f.PickRandom<Condition>())
			.RuleFor(b => b.Weight, f => decimal.Round(f.Random.Decimal(0.3m, 2.5m), 2))
			.RuleFor(b => b.ISBN, f => $"978{f.Random.Long(1000000000L, 9999999999L)}")
			.RuleFor(b => b.Author, f => f.Name.FullName())
			.RuleFor(b => b.Publisher, f => f.Company.CompanyName())
			.RuleFor(b => b.Edition, f => $"{f.Random.Int(2019, 2025)}")
			.RuleFor(b => b.WrittenLanguage, f => f.PickRandom<Enums.LanguageEnum>())
			.RuleFor(b => b.BookCategoryId, f => f.Random.ListItem(bookCategoryIds))
			.RuleFor(b => b.ProductCategoryId, f => f.Random.Bool(0.3f) ? f.Random.ListItem(productCategoryIds) : null);

		var books = booksFaker.Generate(15);

		var servicesFaker = new Faker<AdvertService>("fr_CH")
			.RuleFor(s => s.Title, f => $"Cours de {f.Random.ListItem(subjectList).Name}")
			.RuleFor(s => s.Description, f => f.Lorem.Paragraphs(2))
			.RuleFor(s => s.Price, f => decimal.Round(f.Random.Decimal(20m, 90m), 2))
			.RuleFor(s => s.CreatedAt, f => f.Date.Recent(60, DateTime.UtcNow))
			.RuleFor(s => s.NotificationDate, (f, s) => s.CreatedAt.AddDays(f.Random.Int(3, 20)))
			.RuleFor(s => s.Status, f => f.PickRandom<AdvertStatus>())
			.RuleFor(s => s.UserId, f => f.PickRandom(users).Id)
			.RuleFor(s => s.TeachingLanguage, f => f.PickRandom<Enums.LanguageEnum>())
			.RuleFor(s => s.StudyLevel, f => f.Random.ListItem(schoolGradeList).Name)
			.RuleFor(s => s.SubjectId, f => f.Random.ListItem(subjectList).SubjectId)
			.RuleFor(s => s.SchoolGradeId, f => f.Random.ListItem(schoolGradeList).SchoolGradeId);

		var services = servicesFaker.Generate(18);

		context.Products.AddRange(physicalItems);
		context.Books.AddRange(books);
		context.Services.AddRange(services);
		context.SaveChanges();

        var pictures = new List<Picture>();
        foreach (var item in physicalItems.Cast<PhysicalItem>().Concat(books))
        {
            var count = faker.Random.Int(1, 3);
            for (var i = 1; i <= count; i++)
            {
                pictures.Add(new Picture
                {
                    Label = $"https://picsum.photos/seed/{item.AdvertId}-{i}/800/600",
                    AdvertId = item.AdvertId
                });
            }
        }

        context.Pictures.AddRange(pictures);
        context.SaveChanges();
    }
}
