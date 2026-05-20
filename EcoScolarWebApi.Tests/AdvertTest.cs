using EcoscolarWebApi.Controllers;
using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Services;
using EcoscolarWebApi.Utils.DTOs.Advert;
using EcoscolarWebApi.Utils.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;
using Xunit.Sdk;

namespace EcoScolarWebApi.Tests.Controllers;

public class AdvertsControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly AdvertsController _controller;
    private readonly IAdvertSearchService _searchService; 
    private readonly UserManager<User> _userManagerMock;
    private readonly UsersController _usersController;

    public AdvertsControllerTests()
    {
        _searchService = Substitute.For<IAdvertSearchService>();

        var store = Substitute.For<IUserStore<User>>();
        _userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!); // UserManager requires a lot of dependencies, we can mock them all with NSubstitute

        // Setup InMemory Database context
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        // Simulate the dependency injection of context and store into the AdvertsController
        _usersController = new UsersController(_userManagerMock, _context);
        _controller = new AdvertsController(_context, _searchService);

    }

    public void Dispose()
    {
        // Clean up the in-memory database after each test
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Tests for GetAdverts
    [Fact]
    public async Task GetAdverts_ReturnsAllAdverts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new Books 
            {
                AdvertId = 1,
                Title = "Book Title",
                Description = "Book Descr",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ISBN = "12345",
                Author = "John",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = Language.FR
            },
            new PhysicalItems
            {
                AdvertId = 2,
                Title = "Guitar",
                Description = "Acoustic",
                Price = 120,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.LIKE_NEW
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.Index();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAdverts_ReturnsEmptyList_WhenNoAdverts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.Index();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }
    #endregion

    #region Tests for GetBooks
    [Fact]
    public async Task GetBooks_ReturnsAllBooks()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new Books
            {
                AdvertId = 1,
                Title = "Book Title",
                Description = "Book Descr",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ISBN = "12345",
                Author = "John",
                Publisher = "Pub",
                Edition = "1st",
                WrittenLanguage = Language.FR
            },
            new Books
            {
                AdvertId = 2,
                Title = "Book Title 2",
                Description = "Book Descr 2",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ISBN = "67890",
                Author = "Doe",
                Publisher = "Smith",
                Edition = "3st",
                WrittenLanguage = Language.FR
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetBooks();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.type == "BOOK");
        value.Should().Contain(ad => ad.id == 1 && ad.title == "Book Title" && ad.price == 10);
        value.Should().Contain(ad => ad.id == 2 && ad.title == "Book Title 2" && ad.price == 15.3m);
    }

    [Fact]
    public async Task GetBooks_ReturnsEmptyList_WhenNoBooks()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetBooks();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }
    #endregion

    #region Tests for GetProducts
    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new PhysicalItems
            {
                AdvertId = 2,
                Title = "Guitar 2",
                Description = "Guitar for sale 2",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetProducts();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.type == "PRODUCT");
        value.Should().Contain(ad => ad.id == 1 && ad.title == "Guitar" && ad.price == 10);
        value.Should().Contain(ad => ad.id == 2 && ad.title == "Guitar 2" && ad.price == 15.3m);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProductsWithCategoryId()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var productCategory = new ProductCategories { ProductCategoryId = 1, Name = "Instruments" };
        var productCategory2 = new ProductCategories { ProductCategoryId = 2, Name = "Books" };
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ProductCategoryId = productCategory.ProductCategoryId,
            },
            new PhysicalItems
            {
                AdvertId = 2,
                Title = "Guitar 2",
                Description = "Guitar for sale 2",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ProductCategoryId = productCategory2.ProductCategoryId,
            },
            new PhysicalItems
            {
                AdvertId = 3,
                Title = "Guitar 3",
                Description = "Guitar for sale 3",
                Price = 40,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.USED,
                ProductCategoryId = productCategory.ProductCategoryId,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetProducts(categoryId: productCategory.ProductCategoryId);
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.type == "PRODUCT");
        value.Should().Contain(ad => ad.id == 1 && ad.title == "Guitar" && ad.price == 10);
        value.Should().Contain(ad => ad.id == 3 && ad.title == "Guitar 3" && ad.price == 40);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProductsWithMaxPrice()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new PhysicalItems
            {
                AdvertId = 2,
                Title = "Guitar 2",
                Description = "Guitar for sale 2",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new PhysicalItems
            {
                AdvertId = 3,
                Title = "Guitar 3",
                Description = "Guitar for sale 3",
                Price = 40,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.USED,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetProducts(maxPrice: 20);
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.type == "PRODUCT");
        value.Should().Contain(ad => ad.id == 1 && ad.title == "Guitar" && ad.price == 10);
        value.Should().Contain(ad => ad.id == 2 && ad.title == "Guitar 2" && ad.price == 15.3m);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProductsWithCategoryIdAndMaxPrice()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var productCategory = new ProductCategories { ProductCategoryId = 1, Name = "Instruments" };
        var productCategory2 = new ProductCategories { ProductCategoryId = 2, Name = "Books" };
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ProductCategoryId = productCategory.ProductCategoryId,
            },
            new PhysicalItems
            {
                AdvertId = 2,
                Title = "Guitar 2",
                Description = "Guitar for sale 2",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
                ProductCategoryId = productCategory2.ProductCategoryId,
            },
            new PhysicalItems
            {
                AdvertId = 3,
                Title = "Guitar 3",
                Description = "Guitar for sale 3",
                Price = 40,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.USED,
                ProductCategoryId = productCategory.ProductCategoryId,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetProducts(categoryId: productCategory.ProductCategoryId, maxPrice: 20);
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(1);
        value.Should().OnlyContain(ad => ad.type == "PRODUCT");
        value.Should().Contain(ad => ad.id == 1 && ad.title == "Guitar" && ad.price == 10);
    }

    [Fact]
    public async Task GetProducts_ReturnsEmptyList_WhenNoProducts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetProducts();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }
    #endregion

    #region Tests for GetServices
    [Fact]
    public async Task GetServices_ReturnsAllServices()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new AdvertServices
            {
                AdvertId = 2,
                Title = "Lesson de français",
                Description = "Cours de français pour lycéens",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 2,
                SchoolGradeId = 3,
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetServices();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.type == "SERVICE");
        value.Should().Contain(ad => ad.id == 2 && ad.title == "Lesson de français" && ad.price == 15.3m);
        value.Should().Contain(ad => ad.id == 3 && ad.title == "Lesson de math" && ad.price == 30);
    }

    [Fact]
    public async Task GetServices_ReturnsAllProductsWithResearchKeyword()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new AdvertServices
            {
                AdvertId = 2,
                Title = "Lesson de français",
                Description = "Cours de français pour lycéens",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 2,
                SchoolGradeId = 3,
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetServices(q: "français");
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().HaveCount(1);
        value.Should().OnlyContain(ad => ad.type == "SERVICE");
        value.Should().Contain(ad => ad.id == 2 && ad.title == "Lesson de français" && ad.price == 15.3m);
    }

    [Fact]
    public async Task GetServices_ReturnsAllProductsWithResearchKeywordNotMatching()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var adverts = new List<Adverts>
        {
            new PhysicalItems
            {
                AdvertId = 1,
                Title = "Guitar",
                Description = "Guitar for sale",
                Price = 10,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                Condition = Condition.NEW,
            },
            new AdvertServices
            {
                AdvertId = 2,
                Title = "Lesson de français",
                Description = "Cours de français pour lycéens",
                Price = 15.3m,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 2,
                SchoolGradeId = 3,
            },
            new AdvertServices
            {
                AdvertId = 3,
                Title = "Lesson de math",
                Description = "Cours de math pour lycéens",
                Price = 30,
                UserId = existingUser.Id,
                User = existingUser,
                Status = AdvertStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                NotificationDate = DateTime.UtcNow,
                StudyLevel = "Lycée",
                SubjectId = 1,
                SchoolGradeId = 1,
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetServices(q: "Anglais");
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetServices_ReturnsEmptyList_WhenNoServices()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetServices();
        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }
    #endregion
}
