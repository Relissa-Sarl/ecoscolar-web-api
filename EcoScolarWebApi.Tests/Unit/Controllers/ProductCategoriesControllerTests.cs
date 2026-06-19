using Xunit;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class ProductCategoriesControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly ProductCategoriesController _controller;

    public ProductCategoriesControllerTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);
        _controller = new ProductCategoriesController(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetProductCategories (list)

    [Fact]
    public async Task GetProductCategories_ShouldReturnEmptyList_WhenNoCategories()
    {
        // Act
        var result = await _controller.GetProductCategories();

        // Assert
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductCategories_ShouldReturnAllCategories()
    {
        // Arrange
        _context.ProductCategories.AddRange(
            new ProductCategory { ProductCategoryId = 1, Name = "Electronics", NameFr = "Électronique", NameDe = "Elektronik", NameIt = "Elettronica", Description = "Desc1" },
            new ProductCategory { ProductCategoryId = 2, Name = "Clothing", NameFr = "Vêtements", NameDe = "Kleidung", NameIt = "Abbigliamento", Description = "Desc2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProductCategories();

        // Assert
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region GetProductCategories (by id)

    [Fact]
    public async Task GetProductCategoriesById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _controller.GetProductCategories(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetProductCategoriesById_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        _context.ProductCategories.Add(new ProductCategory
        {
            ProductCategoryId = 1,
            Name = "Electronics",
            NameFr = "Électronique",
            NameDe = "Elektronik",
            NameIt = "Elettronica",
            Description = "Electronic devices"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProductCategories(1);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Electronics");
    }

    #endregion

    #region PostProductCategories

    [Fact]
    public async Task PostProductCategories_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var dto = new ProductCategoryCreateUpdateDto
        {
            Name = "Stationery",
            NameFr = "Papeterie",
            NameDe = "Schreibwaren",
            NameIt = "Cancelleria",
            Description = "Office supplies"
        };

        // Act
        var result = await _controller.PostProductCategories(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var category = createdResult.Value.Should().BeOfType<ProductCategory>().Subject;
        category.Name.Should().Be("Stationery");
    }

    [Fact]
    public async Task PostProductCategories_ShouldPersistToDatabase()
    {
        // Arrange
        var dto = new ProductCategoryCreateUpdateDto
        {
            Name = "Sports",
            NameFr = "Sports",
            NameDe = "Sport",
            NameIt = "Sport",
            Description = "Sports equipment"
        };

        // Act
        await _controller.PostProductCategories(dto);

        // Assert
        var catInDb = await _context.ProductCategories.FirstAsync(c => c.Name == "Sports");
        catInDb.Should().NotBeNull();
        catInDb.Description.Should().Be("Sports equipment");
    }

    #endregion

    #region PutProductCategories

    [Fact]
    public async Task PutProductCategories_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var dto = new ProductCategoryCreateUpdateDto
        {
            Name = "Updated",
            NameFr = "Mise à jour",
            NameDe = "Aktualisiert",
            NameIt = "Aggiornato",
            Description = "Updated desc"
        };

        // Act
        var result = await _controller.PutProductCategories(999, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PutProductCategories_ShouldReturnNoContent_WhenUpdateSucceeds()
    {
        // Arrange
        _context.ProductCategories.Add(new ProductCategory
        {
            ProductCategoryId = 10,
            Name = "Old",
            NameFr = "Ancien",
            NameDe = "Alt",
            NameIt = "Vecchio",
            Description = "Old desc"
        });
        await _context.SaveChangesAsync();

        var dto = new ProductCategoryCreateUpdateDto
        {
            Name = "New",
            NameFr = "Nouveau",
            NameDe = "Neu",
            NameIt = "Nuovo",
            Description = "New desc"
        };

        // Act
        var result = await _controller.PutProductCategories(10, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var catInDb = await _context.ProductCategories.FindAsync(10L);
        catInDb!.Name.Should().Be("New");
        catInDb.Description.Should().Be("New desc");
    }

    #endregion

    #region DeleteProductCategories

    [Fact]
    public async Task DeleteProductCategories_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteProductCategories(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteProductCategories_ShouldReturnNoContent_AndRemoveFromDb()
    {
        // Arrange
        _context.ProductCategories.Add(new ProductCategory
        {
            ProductCategoryId = 20,
            Name = "To Delete",
            NameFr = "À supprimer",
            NameDe = "Zu löschen",
            NameIt = "Da eliminare",
            Description = "Will be deleted"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteProductCategories(20);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var catInDb = await _context.ProductCategories.FindAsync(20L);
        catInDb.Should().BeNull();
    }

    #endregion
}
