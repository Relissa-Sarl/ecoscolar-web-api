using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoScolarWebApi.Tests;

public class LocationsControllerTests
{
    // Helper method to generate a fresh in-memory database for each test
    private async Task<EcoscolarDbContext> GetDbContextAsync(string dbName)
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new EcoscolarDbContext(options);

        // Seed data only if it's empty (In-Memory DBs are cached per 'dbName' during test runs)
        if (!await context.Locations.AnyAsync())
        {
            context.Locations.AddRange(
                new Location { LocationId = 1, PostalCode = "1000", City = "Lausanne", Region = "VD" },
                new Location { LocationId = 2, PostalCode = "1008", City = "Prilly", Region = "VD" },
                new Location { LocationId = 3, PostalCode = "1200", City = "Genève", Region = "GE" },
                new Location { LocationId = 4, PostalCode = "8000", City = "Zürich", Region = "ZH" }
            );

            // Add 20 fake locations to test the .Take(15) limit
            for (int i = 5; i <= 24; i++)
            {
                context.Locations.Add(new Location
                {
                    LocationId = i,
                    PostalCode = "10" + i.ToString("D2"),
                    City = "FakeCity " + i,
                    Region = "VD"
                });
            }

            await context.SaveChangesAsync();
        }

        return context;
    }

    [Fact]
    public async Task SearchLocations_QueryTooShort_ReturnsEmpty()
    {
        // Arrange
        var context = await GetDbContextAsync("Db_TooShort");
        var controller = new LocationsController(context);

        // Act
        var result = await controller.SearchLocations("1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var Enumerable = Assert.IsType<IEnumerable<LocationResponseDto>>(okResult.Value, exactMatch: false);
        Assert.Empty(Enumerable);
    }

    [Fact]
    public async Task SearchLocations_ValidPostalCode_ReturnsMatches()
    {
        // Arrange
        var context = await GetDbContextAsync("Db_PostalCode");
        var controller = new LocationsController(context);

        // Act
        var result = await controller.SearchLocations("120");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsType<IEnumerable<LocationResponseDto>>(okResult.Value, exactMatch: false).ToList();

        Assert.Single(locations);
        Assert.Equal("1200", locations.First().PostalCode);
    }

    [Fact]
    public async Task SearchLocations_ValidCityName_CaseInsensitive_ReturnsMatches()
    {
        // Arrange
        var context = await GetDbContextAsync("Db_CityName");
        var controller = new LocationsController(context);

        // Act
        var result = await controller.SearchLocations("laus"); // Lowercase search

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsType<IEnumerable<LocationResponseDto>>(okResult.Value, exactMatch: false).ToList();

        Assert.NotEmpty(locations);
        Assert.Contains(locations, l => l.City == "Lausanne");
    }

    [Fact]
    public async Task SearchLocations_NoMatch_ReturnsEmpty()
    {
        // Arrange
        var context = await GetDbContextAsync("Db_NoMatch");
        var controller = new LocationsController(context);

        // Act
        var result = await controller.SearchLocations("Paris");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsType<IEnumerable<LocationResponseDto>>(okResult.Value, exactMatch: false);
        Assert.Empty(locations);
    }

    [Fact]
    public async Task SearchLocations_LimitsResultsTo15()
    {
        // Arrange
        var context = await GetDbContextAsync("Db_Limit");
        var controller = new LocationsController(context);

        // Act
        // Searching "10" will match "1000", "1008", and the 20 fake items (1005 to 1024) -> 22 matches total
        var result = await controller.SearchLocations("10");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsType<IEnumerable<LocationResponseDto>>(okResult.Value, exactMatch: false);

        Assert.Equal(15, locations.Count());
    }
}