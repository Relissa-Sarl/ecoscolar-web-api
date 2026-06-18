using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class LanguagesControllerTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly LanguageMapper _mapper;
	private readonly LanguagesController _controller;

	public LanguagesControllerTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);
		_mapper = new LanguageMapper();
		_controller = new LanguagesController(_context, _mapper);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	#region GetLanguages (list)

	[Fact]
	public async Task GetLanguages_ShouldReturnOkWithEmptyList_WhenNoLanguages()
	{
		// Act
		var result = await _controller.GetLanguages();

		// Assert
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var languages = okResult.Value.Should().BeAssignableTo<IEnumerable<LanguageResponse>>().Subject;
		languages.Should().BeEmpty();
	}

	[Fact]
	public async Task GetLanguages_ShouldReturnAllLanguages()
	{
		// Arrange
		_context.Languages.AddRange(
			new Language { Label = "FR", Name = "French", NameFr = "Français", NameDe = "Französisch", NameIt = "Francese" },
			new Language { Label = "DE", Name = "German", NameFr = "Allemand", NameDe = "Deutsch", NameIt = "Tedesco" }
		);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetLanguages();

		// Assert
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var languages = okResult.Value.Should().BeAssignableTo<IEnumerable<LanguageResponse>>().Subject;
		languages.Should().HaveCount(2);
	}

	#endregion

	#region GetLanguages (by label)

	[Fact]
	public async Task GetLanguagesByLabel_ShouldReturnNotFound_WhenLanguageDoesNotExist()
	{
		// Act
		var result = await _controller.GetLanguages("XX");

		// Assert
		result.Result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task GetLanguagesByLabel_ShouldReturnLanguage_WhenExists()
	{
		// Arrange
		_context.Languages.Add(new Language { Label = "FR", Name = "French", NameFr = "Français", NameDe = "Französisch", NameIt = "Francese" });
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetLanguages("FR");

		// Assert
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var lang = okResult.Value.Should().BeOfType<LanguageResponse>().Subject;
		lang.Label.Should().Be("FR");
		lang.Name.Should().Be("French");
	}

	#endregion

	#region PostLanguages

	[Fact]
	public async Task PostLanguages_ShouldReturnCreatedAtAction()
	{
		// Arrange
		var request = new LanguageRequest("IT", "Italian", "Italien", "Italienisch", "Italiano");

		// Act
		var result = await _controller.PostLanguages(request);

		// Assert
		var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
		var response = createdResult.Value.Should().BeOfType<LanguageResponse>().Subject;
		response.Label.Should().Be("IT");
		response.Name.Should().Be("Italian");
	}

	[Fact]
	public async Task PostLanguages_ShouldPersistToDatabase()
	{
		// Arrange
		var request = new LanguageRequest("EN", "English", "Anglais", "Englisch", "Inglese");

		// Act
		await _controller.PostLanguages(request);

		// Assert
		var langInDb = await _context.Languages.FindAsync("EN");
		langInDb.Should().NotBeNull();
		langInDb!.Name.Should().Be("English");
	}

	#endregion

	#region PutLanguages

	[Fact]
	public async Task PutLanguages_ShouldReturnBadRequest_WhenLabelMismatch()
	{
		// Arrange
		var request = new LanguageRequest("DE", "German", "Allemand", "Deutsch", "Tedesco");

		// Act
		var result = await _controller.PutLanguages("FR", request);

		// Assert
		result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task PutLanguages_ShouldReturnNotFound_WhenLanguageDoesNotExist()
	{
		// Arrange
		var request = new LanguageRequest("XX", "Unknown", "Inconnu", "Unbekannt", "Sconosciuto");

		// Act
		var result = await _controller.PutLanguages("XX", request);

		// Assert
		result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task PutLanguages_ShouldReturnNoContent_WhenUpdateSucceeds()
	{
		// Arrange
		_context.Languages.Add(new Language { Label = "FR", Name = "Old French", NameFr = "Ancien français", NameDe = "Alt Französisch", NameIt = "Vecchio francese" });
		await _context.SaveChangesAsync();

		var request = new LanguageRequest("FR", "French", "Français", "Französisch", "Francese");

		// Act
		var result = await _controller.PutLanguages("FR", request);

		// Assert
		result.Should().BeOfType<NoContentResult>();

		var langInDb = await _context.Languages.FindAsync("FR");
		langInDb!.Name.Should().Be("French");
	}

	#endregion

	#region DeleteLanguages

	[Fact]
	public async Task DeleteLanguages_ShouldReturnNotFound_WhenLanguageDoesNotExist()
	{
		// Act
		var result = await _controller.DeleteLanguages("XX");

		// Assert
		result.Should().BeOfType<NotFoundResult>();
	}

	[Fact]
	public async Task DeleteLanguages_ShouldReturnNoContent_AndRemoveFromDb()
	{
		// Arrange
		_context.Languages.Add(new Language { Label = "RM", Name = "Romansh", NameFr = "Romanche", NameDe = "Rätoromanisch", NameIt = "Romancio" });
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.DeleteLanguages("RM");

		// Assert
		result.Should().BeOfType<NoContentResult>();
		var langInDb = await _context.Languages.FindAsync("RM");
		langInDb.Should().BeNull();
	}

	#endregion
}
