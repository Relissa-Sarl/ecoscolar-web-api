using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using LanguageEnum = EcoScolarWebApi.Enums.LanguageEnum;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// T8-3 · intégration création annonce livre (AdvertsController + EF InMemory).
/// </summary>
public class BookCreateIntegrationTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly AdvertsController _controller;

	public BookCreateIntegrationTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		_context = new EcoscolarDbContext(options);
		_controller = new AdvertsController(_context, Substitute.For<IAdvertSearchService>());
	}

	[Fact]
	public async Task CreateBook_WithValidPayload_PersistsBookInDatabase()
	{
		_context.BookCategories.Add(new BookCategory
		{
			BookCategoryId = 1,
			Name = "Sciences",
			Description = "Manuels scientifiques"
		});
		await _context.SaveChangesAsync();

		var dto = new BookCreateDto(
			Title: "Manuel de biologie",
			Description: "Livre pour le gymnase.",
			Price: 25m,
			UserId: "integration-user",
			Images: ["https://example.com/cover.jpg"],
			Condition: PhysicalItemCondition.NEW,
			CategoryId: 1,
			Isbn: "978-3-16-148410-0",
			Author: "A. Dupont",
			Publisher: "Helbing",
			Edition: "3e",
			WrittenLanguage: LanguageEnum.FR
		);

		var result = await _controller.CreateBook(dto);

		var created = result.Result as CreatedAtActionResult;
		created.Should().NotBeNull();

		var books = await _context.Books.ToListAsync();
		books.Should().ContainSingle(b => b.Title == "Manuel de biologie" && b.ISBN == "978-3-16-148410-0");
	}

	[Fact]
	public async Task CreateBook_WithInvalidTitle_ReturnsBadRequest()
	{
		_context.BookCategories.Add(new BookCategory
		{
			BookCategoryId = 1,
			Name = "Sciences",
			Description = "Manuels scientifiques"
		});
		await _context.SaveChangesAsync();

		var dto = new BookCreateDto(
			Title: "",
			Description: "Sans titre",
			Price: 10m,
			UserId: "integration-user",
			Images: [],
			Condition: PhysicalItemCondition.NEW,
			CategoryId: 1,
			Isbn: "9780000000000",
			Author: "A",
			Publisher: "P",
			Edition: "1",
			WrittenLanguage: LanguageEnum.FR
		);

		_controller.ModelState.AddModelError("Title", "Required");

		var result = await _controller.CreateBook(dto);

		result.Result.Should().BeOfType<BadRequestObjectResult>();
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
	}
}
