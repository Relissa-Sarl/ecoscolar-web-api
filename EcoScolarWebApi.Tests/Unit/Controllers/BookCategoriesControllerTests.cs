using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class BookCategoriesControllerTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly BookCategoriesController _controller;

	public BookCategoriesControllerTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);
		_controller = new BookCategoriesController(_context);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	#region GetBookCategories (list)

	[Fact]
	public async Task GetBookCategories_ShouldReturnEmptyList_WhenNoCategories()
	{
		// Act
		var result = await _controller.GetBookCategories();

		// Assert — controller returns list directly via implicit ActionResult conversion
		result.Value.Should().NotBeNull();
		result.Value!.Should().BeEmpty();
	}

	[Fact]
	public async Task GetBookCategories_ShouldReturnAllCategories()
	{
		// Arrange
		_context.BookCategories.AddRange(
			new BookCategory { BookCategoryId = 1, Name = "Fiction", NameFr = "Fiction", NameDe = "Fiktion", NameIt = "Narrativa", Description = "Fiction books" },
			new BookCategory { BookCategoryId = 2, Name = "Science", NameFr = "Sciences", NameDe = "Wissenschaft", NameIt = "Scienza", Description = "Science books" }
		);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetBookCategories();

		// Assert
		var categories = result.Value;
		categories.Should().NotBeNull();
		categories!.Should().HaveCount(2);
	}

	#endregion

	#region GetBookCategories (by id)

	[Fact]
	public async Task GetBookCategoriesById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
	{
		// Act
		var result = await _controller.GetBookCategories(999);

		// Assert
		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task GetBookCategoriesById_ShouldReturnCategory_WhenExists()
	{
		// Arrange
		var category = new BookCategory
		{
			BookCategoryId = 1,
			Name = "Fiction",
			NameFr = "Fiction",
			NameDe = "Fiktion",
			NameIt = "Narrativa",
			Description = "Fiction books"
		};
		_context.BookCategories.Add(category);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetBookCategories(1);

		// Assert
		result.Value.Should().NotBeNull();
		result.Value!.Name.Should().Be("Fiction");
	}

	#endregion

	#region PostBookCategories

	[Fact]
	public async Task PostBookCategories_ShouldReturnCreatedAtAction()
	{
		// Arrange
		var dto = new BookCategoryCreateUpdateDto
		{
			Name = "History",
			NameFr = "Histoire",
			NameDe = "Geschichte",
			NameIt = "Storia",
			Description = "History books"
		};

		// Act
		var result = await _controller.PostBookCategories(dto);

		// Assert
		var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
		var createdCategory = createdResult.Value.Should().BeOfType<BookCategory>().Subject;
		createdCategory.Name.Should().Be("History");
		createdCategory.NameFr.Should().Be("Histoire");
	}

	[Fact]
	public async Task PostBookCategories_ShouldPersistToDatabase()
	{
		// Arrange
		var dto = new BookCategoryCreateUpdateDto
		{
			Name = "Math",
			NameFr = "Mathématiques",
			NameDe = "Mathematik",
			NameIt = "Matematica",
			Description = "Math textbooks"
		};

		// Act
		await _controller.PostBookCategories(dto);

		// Assert
		var catInDb = await _context.BookCategories.FirstAsync(c => c.Name == "Math");
		catInDb.Should().NotBeNull();
		catInDb.Description.Should().Be("Math textbooks");
	}

	#endregion

	#region PutBookCategories

	[Fact]
	public async Task PutBookCategories_ShouldReturnNotFound_WhenCategoryDoesNotExist()
	{
		// Arrange
		var dto = new BookCategoryCreateUpdateDto
		{
			Name = "Updated",
			NameFr = "Mise à jour",
			NameDe = "Aktualisiert",
			NameIt = "Aggiornato",
			Description = "Updated desc"
		};

		// Act
		var result = await _controller.PutBookCategories(999, dto);

		// Assert
		result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task PutBookCategories_ShouldReturnNoContent_WhenUpdateSucceeds()
	{
		// Arrange
		var category = new BookCategory
		{
			BookCategoryId = 10,
			Name = "Old Name",
			NameFr = "Ancien nom",
			NameDe = "Alter Name",
			NameIt = "Vecchio nome",
			Description = "Old desc"
		};
		_context.BookCategories.Add(category);
		await _context.SaveChangesAsync();

		var dto = new BookCategoryCreateUpdateDto
		{
			Name = "New Name",
			NameFr = "Nouveau nom",
			NameDe = "Neuer Name",
			NameIt = "Nuovo nome",
			Description = "New desc"
		};

		// Act
		var result = await _controller.PutBookCategories(10, dto);

		// Assert
		result.Should().BeOfType<NoContentResult>();

		var updatedInDb = await _context.BookCategories.FindAsync(10L);
		updatedInDb!.Name.Should().Be("New Name");
		updatedInDb.Description.Should().Be("New desc");
	}

	#endregion

	#region DeleteBookCategories

	[Fact]
	public async Task DeleteBookCategories_ShouldReturnNotFound_WhenCategoryDoesNotExist()
	{
		// Act
		var result = await _controller.DeleteBookCategories(999);

		// Assert
		result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task DeleteBookCategories_ShouldReturnNoContent_AndRemoveFromDb()
	{
		// Arrange
		var category = new BookCategory
		{
			BookCategoryId = 20,
			Name = "To Delete",
			NameFr = "À supprimer",
			NameDe = "Zu löschen",
			NameIt = "Da eliminare",
			Description = "Will be deleted"
		};
		_context.BookCategories.Add(category);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.DeleteBookCategories(20);

		// Assert
		result.Should().BeOfType<NoContentResult>();
		var catInDb = await _context.BookCategories.FindAsync(20L);
		catInDb.Should().BeNull();
	}

	#endregion
}
