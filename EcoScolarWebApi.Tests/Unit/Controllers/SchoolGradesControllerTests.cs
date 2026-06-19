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

public class SchoolGradesControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly SchoolGradesController _controller;

    public SchoolGradesControllerTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);
        _controller = new SchoolGradesController(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetSchoolGrades (list)

    [Fact]
    public async Task GetSchoolGrades_ShouldReturnEmptyList_WhenNoGrades()
    {
        // Act
        var result = await _controller.GetSchoolGrades();

        // Assert
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSchoolGrades_ShouldReturnAllGrades()
    {
        // Arrange
        _context.SchoolGrades.AddRange(
            new SchoolGrade { SchoolGradeId = 1, Name = "Grade 1", Code = "G1", NameFr = "1ère année", NameDe = "1. Klasse", NameIt = "1° anno" },
            new SchoolGrade { SchoolGradeId = 2, Name = "Grade 2", Code = "G2", NameFr = "2ème année", NameDe = "2. Klasse", NameIt = "2° anno" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSchoolGrades();

        // Assert
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region GetSchoolGrades (by id)

    [Fact]
    public async Task GetSchoolGradesById_ShouldReturnNotFound_WhenGradeDoesNotExist()
    {
        // Act
        var result = await _controller.GetSchoolGrades(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSchoolGradesById_ShouldReturnGrade_WhenExists()
    {
        // Arrange
        _context.SchoolGrades.Add(new SchoolGrade
        {
            SchoolGradeId = 1,
            Name = "Grade 1",
            Code = "G1",
            NameFr = "1ère année",
            NameDe = "1. Klasse",
            NameIt = "1° anno"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSchoolGrades(1);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Grade 1");
        result.Value.Code.Should().Be("G1");
    }

    #endregion

    #region PostSchoolGrades

    [Fact]
    public async Task PostSchoolGrades_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var dto = new SchoolGradeCreateUpdateDto
        {
            Name = "Grade 3",
            SchoolGrade = "G3",
            NameFr = "3ème année",
            NameDe = "3. Klasse",
            NameIt = "3° anno"
        };

        // Act
        var result = await _controller.PostSchoolGrades(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var grade = createdResult.Value.Should().BeOfType<SchoolGrade>().Subject;
        grade.Name.Should().Be("Grade 3");
        grade.Code.Should().Be("G3");
    }

    [Fact]
    public async Task PostSchoolGrades_ShouldPersistToDatabase()
    {
        // Arrange
        var dto = new SchoolGradeCreateUpdateDto
        {
            Name = "Grade 4",
            SchoolGrade = "G4",
            NameFr = "4ème année",
            NameDe = "4. Klasse",
            NameIt = "4° anno"
        };

        // Act
        await _controller.PostSchoolGrades(dto);

        // Assert
        var gradeInDb = await _context.SchoolGrades.FirstAsync(g => g.Code == "G4");
        gradeInDb.Should().NotBeNull();
        gradeInDb.Name.Should().Be("Grade 4");
    }

    #endregion

    #region PutSchoolGrades

    [Fact]
    public async Task PutSchoolGrades_ShouldReturnNotFound_WhenGradeDoesNotExist()
    {
        // Arrange
        var dto = new SchoolGradeCreateUpdateDto
        {
            Name = "Updated",
            SchoolGrade = "UPD",
            NameFr = "Mise à jour",
            NameDe = "Aktualisiert",
            NameIt = "Aggiornato"
        };

        // Act
        var result = await _controller.PutSchoolGrades(999, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PutSchoolGrades_ShouldReturnNoContent_WhenUpdateSucceeds()
    {
        // Arrange
        _context.SchoolGrades.Add(new SchoolGrade
        {
            SchoolGradeId = 10,
            Name = "Old Grade",
            Code = "OG",
            NameFr = "Ancien",
            NameDe = "Alt",
            NameIt = "Vecchio"
        });
        await _context.SaveChangesAsync();

        var dto = new SchoolGradeCreateUpdateDto
        {
            Name = "New Grade",
            SchoolGrade = "NG",
            NameFr = "Nouveau",
            NameDe = "Neu",
            NameIt = "Nuovo"
        };

        // Act
        var result = await _controller.PutSchoolGrades(10, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var gradeInDb = await _context.SchoolGrades.FindAsync(10L);
        gradeInDb!.Name.Should().Be("New Grade");
        gradeInDb.Code.Should().Be("NG");
    }

    #endregion

    #region DeleteSchoolGrades

    [Fact]
    public async Task DeleteSchoolGrades_ShouldReturnNotFound_WhenGradeDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteSchoolGrades(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSchoolGrades_ShouldReturnNoContent_AndRemoveFromDb()
    {
        // Arrange
        _context.SchoolGrades.Add(new SchoolGrade
        {
            SchoolGradeId = 20,
            Name = "To Delete",
            Code = "DEL",
            NameFr = "À supprimer",
            NameDe = "Zu löschen",
            NameIt = "Da eliminare"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSchoolGrades(20);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var gradeInDb = await _context.SchoolGrades.FindAsync(20L);
        gradeInDb.Should().BeNull();
    }

    #endregion
}
