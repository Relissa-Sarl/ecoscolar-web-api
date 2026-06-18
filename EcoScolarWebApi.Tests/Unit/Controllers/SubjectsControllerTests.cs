using Xunit;
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

public class SubjectsControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly SubjectMapper _mapper;
    private readonly SubjectsController _controller;

    public SubjectsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);
        _mapper = new SubjectMapper();
        _controller = new SubjectsController(_context, _mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetSubjects (list)

    [Fact]
    public async Task GetSubjects_ShouldReturnOkWithEmptyList_WhenNoSubjects()
    {
        // Act
        var result = await _controller.GetSubjects();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var subjects = okResult.Value.Should().BeAssignableTo<IEnumerable<SubjectResponseDTO>>().Subject;
        subjects.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubjects_ShouldReturnAllSubjects()
    {
        // Arrange
        _context.Subjects.AddRange(
            new Subject { SubjectId = 1, Name = "Math", Code = "MATH", NameFr = "Mathématiques", NameDe = "Mathematik", NameIt = "Matematica" },
            new Subject { SubjectId = 2, Name = "Physics", Code = "PHYS", NameFr = "Physique", NameDe = "Physik", NameIt = "Fisica" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSubjects();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var subjects = okResult.Value.Should().BeAssignableTo<IEnumerable<SubjectResponseDTO>>().Subject;
        subjects.Should().HaveCount(2);
    }

    #endregion

    #region GetSubjects (by id)

    [Fact]
    public async Task GetSubjectsById_ShouldReturnNotFound_WhenSubjectDoesNotExist()
    {
        // Act
        var result = await _controller.GetSubjects(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubjectsById_ShouldReturnSubject_WhenExists()
    {
        // Arrange
        _context.Subjects.Add(new Subject
        {
            SubjectId = 1,
            Name = "Math",
            Code = "MATH",
            NameFr = "Mathématiques",
            NameDe = "Mathematik",
            NameIt = "Matematica"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSubjects(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var subject = okResult.Value.Should().BeOfType<SubjectResponseDTO>().Subject;
        subject.Name.Should().Be("Math");
        subject.Code.Should().Be("MATH");
    }

    #endregion

    #region PostSubjects

    [Fact]
    public async Task PostSubjects_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new SubjectRequestDTO("Chemistry", "CHEM", "Chimie", "Chemie", "Chimica");

        // Act
        var result = await _controller.PostSubjects(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<SubjectResponseDTO>().Subject;
        response.Name.Should().Be("Chemistry");
        response.Code.Should().Be("CHEM");
    }

    [Fact]
    public async Task PostSubjects_ShouldPersistToDatabase()
    {
        // Arrange
        var request = new SubjectRequestDTO("Biology", "BIO", "Biologie", "Biologie", "Biologia");

        // Act
        await _controller.PostSubjects(request);

        // Assert
        var subjectInDb = await _context.Subjects.FirstAsync(s => s.Code == "BIO");
        subjectInDb.Should().NotBeNull();
        subjectInDb.Name.Should().Be("Biology");
    }

    #endregion

    #region PutSubjects

    [Fact]
    public async Task PutSubjects_ShouldReturnNotFound_WhenSubjectDoesNotExist()
    {
        // Arrange
        var request = new SubjectRequestDTO("Updated", "UPD", "Mise à jour", "Aktualisiert", "Aggiornato");

        // Act
        var result = await _controller.PutSubjects(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PutSubjects_ShouldReturnNoContent_WhenUpdateSucceeds()
    {
        // Arrange
        _context.Subjects.Add(new Subject
        {
            SubjectId = 10,
            Name = "Old Name",
            Code = "OLD",
            NameFr = "Ancien",
            NameDe = "Alt",
            NameIt = "Vecchio"
        });
        await _context.SaveChangesAsync();

        var request = new SubjectRequestDTO("New Name", "NEW", "Nouveau", "Neu", "Nuovo");

        // Act
        var result = await _controller.PutSubjects(10, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var subjectInDb = await _context.Subjects.FindAsync(10L);
        subjectInDb!.Name.Should().Be("New Name");
        subjectInDb.Code.Should().Be("NEW");
    }

    #endregion

    #region DeleteSubjects

    [Fact]
    public async Task DeleteSubjects_ShouldReturnNotFound_WhenSubjectDoesNotExist()
    {
        // Act
        var result = await _controller.DeleteSubjects(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSubjects_ShouldReturnNoContent_AndRemoveFromDb()
    {
        // Arrange
        _context.Subjects.Add(new Subject
        {
            SubjectId = 20,
            Name = "To Delete",
            Code = "DEL",
            NameFr = "À supprimer",
            NameDe = "Zu löschen",
            NameIt = "Da eliminare"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSubjects(20);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var subjectInDb = await _context.Subjects.FindAsync(20L);
        subjectInDb.Should().BeNull();
    }

    #endregion
}
