using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;
using LanguageEnum = EcoScolarWebApi.Enums.LanguageEnum;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.DTOs.Adverts;

namespace EcoScolarWebApi.Tests.Controllers;

public class AdvertsControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly AdvertsController _controller;
    private readonly IAdvertSearchService _searchService; 
    private readonly UserManager<User> _userManagerMock;
    private readonly UsersController _usersController;
    private readonly IUserService _userServiceMock;

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
		_userServiceMock = Substitute.For<IUserService>();

		// Simulate the dependency injection of context and store into the AdvertsController
		_usersController = new UsersController(_userServiceMock, _userManagerMock, _context);
		_controller = new AdvertsController(_context, _searchService);
    }

    public void Dispose()
    {
        // Clean up the in-memory database after each test
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Tests for Index
    [Fact]
    public async Task Index_ReturnsAllAdverts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 },
            new Picture { PictureId = 4, Label = "http://example.com/pic2.jpg", AdvertId = 2 }
        };

        var adverts = new List<Advert>
        {
            new Book 
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
                WrittenLanguage = LanguageEnum.FR,
                Pictures = pictures
            },
            new PhysicalItem
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
                Condition = Condition.LIKE_NEW,
                Pictures = pictures2
            },
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Index_ReturnsEmptyList_WhenNoAdverts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        // Act
        var result = await _controller.Index();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().BeEmpty("The controller did not return an empty list");
    }
    #endregion

    #region Tests for GetBooks
    [Fact]
    public async Task GetBooks_ReturnsAllBooks()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 }
        };
        var adverts = new List<Advert>
        {
            new Book
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
                WrittenLanguage = LanguageEnum.FR,
                Pictures = pictures
            },
            new Book
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
                WrittenLanguage = LanguageEnum.FR,
                Pictures = pictures2
            },
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.Type == "BOOK");
        value.Should().Contain(ad => ad.Id == 1 && ad.Title == "Book Title" && ad.Price == 10);
        value.Should().Contain(ad => ad.Id == 2 && ad.Title == "Book Title 2" && ad.Price == 15.3m);
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().BeEmpty("The controller did not return an empty list");
    }
    #endregion

    #region Tests for GetProducts
    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new PhysicalItem
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
                Pictures = pictures2
            },
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.Type == "PRODUCT");
        value.Should().Contain(ad => ad.Id == 1 && ad.Title == "Guitar" && ad.Price == 10);
        value.Should().Contain(ad => ad.Id == 2 && ad.Title == "Guitar 2" && ad.Price == 15.3m);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts_WhenCategoryIdSpecified()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var productCategory = new ProductCategory { ProductCategoryId = 1, Name = "Instruments" };
        var productCategory2 = new ProductCategory { ProductCategoryId = 2, Name = "Books" };
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 }
        };
        List<Picture> pictures3 = new List<Picture>
        {
            new Picture { PictureId = 4, Label = "http://example.com/pic1.jpg", AdvertId = 3 },
            new Picture { PictureId = 5, Label = "http://example.com/pic2.jpg", AdvertId = 3 },
            new Picture { PictureId = 6, Label = "http://example.com/pic3.jpg", AdvertId = 3 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new PhysicalItem
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
                Pictures = pictures2
            },
            new PhysicalItem
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
                Pictures = pictures3
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProducts(categoryId: productCategory.ProductCategoryId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.Type == "PRODUCT");
        value.Should().Contain(ad => ad.Id == 1 && ad.Title == "Guitar" && ad.Price == 10);
        value.Should().Contain(ad => ad.Id == 3 && ad.Title == "Guitar 3" && ad.Price == 40);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts_WhenMaxPriceSpecified()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 }
        };
        List<Picture> pictures3 = new List<Picture>
        {
            new Picture { PictureId = 4, Label = "http://example.com/pic1.jpg", AdvertId = 3 },
            new Picture { PictureId = 5, Label = "http://example.com/pic2.jpg", AdvertId = 3 },
            new Picture { PictureId = 6, Label = "http://example.com/pic3.jpg", AdvertId = 3 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new PhysicalItem
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
                Pictures = pictures2
            },
            new PhysicalItem
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
                Pictures = pictures3
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProducts(maxPrice: 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.Type == "PRODUCT");
        value.Should().Contain(ad => ad.Id == 1 && ad.Title == "Guitar" && ad.Price == 10);
        value.Should().Contain(ad => ad.Id == 2 && ad.Title == "Guitar 2" && ad.Price == 15.3m);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts_WhenCategoryIdAndMaxPriceSpecified()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var productCategory = new ProductCategory { ProductCategoryId = 1, Name = "Instruments" };
        var productCategory2 = new ProductCategory { ProductCategoryId = 2, Name = "Books" };
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        List<Picture> pictures2 = new List<Picture>
        {
            new Picture { PictureId = 3, Label = "http://example.com/pic1.jpg", AdvertId = 2 }
        };
        List<Picture> pictures3 = new List<Picture>
        {
            new Picture { PictureId = 4, Label = "http://example.com/pic1.jpg", AdvertId = 3 },
            new Picture { PictureId = 5, Label = "http://example.com/pic2.jpg", AdvertId = 3 },
            new Picture { PictureId = 6, Label = "http://example.com/pic3.jpg", AdvertId = 3 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new PhysicalItem
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
                Pictures = pictures2
            },
            new PhysicalItem
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
                Pictures = pictures3
            }
        };
        _context.Adverts.AddRange(adverts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProducts(categoryId: productCategory.ProductCategoryId, maxPrice: 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(1);
        value.Should().OnlyContain(ad => ad.Type == "PRODUCT");
        value.Should().Contain(ad => ad.Id == 1 && ad.Title == "Guitar" && ad.Price == 10);
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().BeEmpty("The controller did not return an empty list");
    }
    #endregion

    #region Tests for GetServices
    [Fact]
    public async Task GetServices_ReturnsAllServices()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new TutoringAdvert
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
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(2);
        value.Should().OnlyContain(ad => ad.Type == "SERVICE");
        value.Should().Contain(ad => ad.Id == 2 && ad.Title == "Lesson de français" && ad.Price == 15.3m);
        value.Should().Contain(ad => ad.Id == 3 && ad.Title == "Lesson de math" && ad.Price == 30);
    }

    [Fact]
    public async Task GetServices_ReturnsAllServices_WhenResearchKeyword()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new TutoringAdvert
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
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().HaveCount(1);
        value.Should().OnlyContain(ad => ad.Type == "SERVICE");
        value.Should().Contain(ad => ad.Id == 2 && ad.Title == "Lesson de français" && ad.Price == 15.3m);
    }

    [Fact]
    public async Task GetServices_ReturnEmptyList_WhenResearchKeywordNotMatching()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var adverts = new List<Advert>
        {
            new PhysicalItem
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
                Pictures = pictures
            },
            new TutoringAdvert
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
            new TutoringAdvert
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().BeEmpty("The controller did not return an empty list");
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
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");
        var value = okResult!.Value as IEnumerable<AdvertReadDto>;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value.Should().BeEmpty("The controller did not return an empty list");
    }
    #endregion

    #region Tests for Details
    [Fact]
    public async Task Details_ReturnsAdvert_WhenAdvertExists()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Details(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as AdvertReadDto;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value!.Id.Should().Be(1);
        value.Title.Should().Be("Book Title");
        value.Price.Should().Be(10);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenAdvertDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.Details(999); // Non-existing ID
        // Assert
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for GetBookById
    [Fact]
    public async Task GetBookById_ReturnsBook_WhenBookExists()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var grade = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(grade);

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetBookById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as BookReadDto;
        value.Should().NotBeNull("The controller did not return a BookReadDto");
        value!.id.Should().Be(1);
        value.title.Should().Be("Book Title");
        value.price.Should().Be(10);
    }

    [Fact]
    public async Task GetBookById_ReturnsNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetBookById(999); // Non-existing ID
        // Assert
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }

    [Fact]
    public async Task GetBookById_ReturnsNotFound_WhenIdIsWrong()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new TutoringAdvert
        {
            AdvertId = 1,
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
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetBookById(1);

        // Assert
        var okResult = result.Result as NotFoundResult;
        okResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for GetProductById
    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenProductExists()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new PhysicalItem
        {
            AdvertId = 1,
            Title = "Guitar",
            Description = "Guitar for sale",
            Price = 40,
            UserId = existingUser.Id,
            User = existingUser,
            Status = AdvertStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            NotificationDate = DateTime.UtcNow,
            Condition = Condition.NEW,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProductById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as ProductReadDto;
        value.Should().NotBeNull("The controller did not return a ProductReadDto");
        value!.Id.Should().Be(1);
        value.Title.Should().Be("Guitar");
        value.Price.Should().Be(40);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetProductById(999); // Non-existing ID
        // Assert
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenIdIsWrong()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var advert = new TutoringAdvert
        {
            AdvertId = 1,
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
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetProductById(1);

        // Assert
        var okResult = result.Result as NotFoundResult;
        okResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for GetServiceById
    [Fact]
    public async Task GetServiceById_ReturnsService_WhenServiceExists()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var subject = new Subject { SubjectId = 1, Name = "Mathématiques", Code = "Maths" };
        var grade = new SchoolGrade { SchoolGradeId = 1, Name = "Terminale", Code = "Lycée" };

        _context.Set<Subject>().Add(subject);
        _context.Set<SchoolGrade>().Add(grade);
        var advert = new TutoringAdvert
        {
            AdvertId = 1,
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
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetServiceById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("The controller did not return an OkObjectResult");

        var value = okResult!.Value as ServiceReadDto;
        value.Should().NotBeNull("The controller did not return a ServiceReadDto");
        value!.Id.Should().Be(1);
        value.Title.Should().Be("Lesson de math");
        value.Price.Should().Be(30);
    }

    [Fact]
    public async Task GetServiceById_ReturnsNotFound_WhenServiceDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        // Act
        var result = await _controller.GetServiceById(999); // Non-existing ID
        // Assert
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }

    [Fact]
    public async Task GetServiceById_ReturnsNotFound_WhenIdIsWrong()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new PhysicalItem
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
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetServiceById(1);

        // Assert
        var okResult = result.Result as NotFoundResult;
        okResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for GetSummaries
    [Fact]
    public async Task GetSummaries_ReturnsOk_WithListOfSummaries()
    {
        // Arrange
        var query = new AdvertSearchQuery { Q = "Math" };
        var expectedSummaries = new List<AdvertSummaryDto> { new AdvertSummaryDto { Id = Guid.NewGuid() } };

        _searchService.SearchSummariesAsync(query, Arg.Any<CancellationToken>())
            .Returns(expectedSummaries);

        // Act
        var result = await _controller.GetSummaries(query, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedSummaries);
    }
    #endregion

    #region Tests for GetSummaryDetail
    [Fact]
    public async Task GetSummaryDetail_ReturnsOk_WhenItemExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedDetail = new AdvertDetailDto { Id = id };

        _searchService.GetDetailAsync(id, Arg.Any<CancellationToken>())
            .Returns(expectedDetail);

        // Act
        var result = await _controller.GetSummaryDetail(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedDetail);
    }

    [Fact]
    public async Task GetSummaryDetail_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _searchService.GetDetailAsync(id, Arg.Any<CancellationToken>())
            .Returns((AdvertDetailDto?)null);

        // Act
        var result = await _controller.GetSummaryDetail(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
    #endregion

    #region Tests for CreateBook
    [Fact]
    public async Task CreateBook_ReturnsCreatedBook_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var bookCategory = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(bookCategory);
        await _context.SaveChangesAsync();

        var bookCreateDto = new BookCreateDto(
            Title: "New Book",
            Description: "Description of the new book",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            CategoryId: 1,
            Isbn: "54321",
            Author: "Jane",
            Publisher: "New Pub",
            Edition: "2nd",
            WrittenLanguage: LanguageEnum.FR
        );

        // Act
        var result = await _controller.CreateBook(bookCreateDto);

        // Assert
        var createdAtActionResult = result.Result as CreatedAtActionResult;
        createdAtActionResult.Should().NotBeNull("The controller did not return a CreatedAtActionResult");

        var value = createdAtActionResult!.Value as AdvertReadDto;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value!.Title.Should().Be("New Book");
        value.Price.Should().Be(20);
    }

    [Fact]
    public async Task CreateBook_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var bookCategory = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(bookCategory);
        await _context.SaveChangesAsync();

        var bookCreateDto = new BookCreateDto(
            Title: "", // Invalid
            Description: "Description of the new book",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
            new Picture { Label = "http://example.com/newpic1.jpg" },
            new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            CategoryId: 1,
            Isbn: "54321",
            Author: "Jane",
            Publisher: "New Pub",
            Edition: "2nd",
            WrittenLanguage: LanguageEnum.FR
        );

        // FORCE VALIDATION ERROR
        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.CreateBook(bookCreateDto);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }
    #endregion

    #region Tests for CreateProduct
    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var Category = new ProductCategory{ ProductCategoryId = 1, Name = "Guitare", Description = "Guitares" };
        _context.Set<ProductCategory>().Add(Category);
        await _context.SaveChangesAsync();

        var productCreateDto = new ProductCreateDto(
            Title: "New Product",
            Description: "Description of the new product",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            ProductCategoryId: 1
        );

        // Act
        var result = await _controller.CreateProduct(productCreateDto);

        // Assert
        var createdAtActionResult = result.Result as CreatedAtActionResult;
        createdAtActionResult.Should().NotBeNull("The controller did not return a CreatedAtActionResult");

        var value = createdAtActionResult!.Value as AdvertReadDto;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value!.Title.Should().Be("New Product");
        value.Price.Should().Be(20);
    }

    [Fact]
    public async Task CreateProduct_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var productCategory = new ProductCategory { ProductCategoryId = 1, Name = "Guitare", Description = "Guitares" };
        _context.Set<ProductCategory>().Add(productCategory);
        await _context.SaveChangesAsync();

        var productCreateDto = new ProductCreateDto(
            Title: "", // Invalid
            Description: "Description of the new product",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
            new Picture { Label = "http://example.com/newpic1.jpg" },
            new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            ProductCategoryId: 1
        );

        // FORCE VALIDATION ERROR
        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.CreateProduct(productCreateDto);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }
    #endregion

    #region Tests for CreateService
    [Fact]
    public async Task CreateService_ReturnsCreatedService_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var subject = new Subject { SubjectId = 1, Name = "Math", Code = "Mathématiques" };
        _context.Set<Subject>().Add(subject);

        var grade = new SchoolGrade { SchoolGradeId = 1, Name = "Terminale", Code = "Lycée" };
        _context.Set<SchoolGrade>().Add(grade);
        await _context.SaveChangesAsync();

        var serviceCreateDto = new ServiceCreateDto(
            Title: "New Service",
            Description: "Description of the new service",
            Price: 20m,
            UserId: existingUser.Id,
            SubjectId: 1,
            SchoolLevelId: 1,
            TeachingLanguage: LanguageEnum.FR,
            SpecificStudyLevel: "Diplôme en Mathématiques"
        );

        // Act
        var result = await _controller.CreateService(serviceCreateDto);

        // Assert
        var createdAtActionResult = result.Result as CreatedAtActionResult;
        createdAtActionResult.Should().NotBeNull("The controller did not return a CreatedAtActionResult");

        var value = createdAtActionResult!.Value as AdvertReadDto;
        value.Should().NotBeNull("The controller did not return an AdvertReadDto");
        value!.Title.Should().Be("New Service");
        value.Price.Should().Be(20);
    }

    [Fact]
    public async Task CreateService_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var subject = new Subject { SubjectId = 1, Name = "Math", Code = "Mathématiques" };
        _context.Set<Subject>().Add(subject);

        var grade = new SchoolGrade { SchoolGradeId = 1, Name = "Terminale", Code = "Lycée" };
        _context.Set<SchoolGrade>().Add(grade);
        await _context.SaveChangesAsync();

        var serviceCreateDto = new ServiceCreateDto(
            Title: "", // Invalid
            Description: "Description of the new service",
            Price: 20m,
            UserId: existingUser.Id,
            SubjectId: 1,
            SchoolLevelId: 1,
            TeachingLanguage: LanguageEnum.FR,
            SpecificStudyLevel: "Diplôme en Mathématiques"
        );

        // FORCE VALIDATION ERROR
        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.CreateService(serviceCreateDto);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }
    #endregion

    #region Tests for EditBook
    [Fact]
    public async Task EditBook_ReturnsUpdatedBook_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var bookCategory = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(bookCategory);
        await _context.SaveChangesAsync();

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };

        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var bookUpdateDto = new BookCreateDto(
            Title: "New Book Title",
            Description: "New description for the book",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            CategoryId: 1,
            Isbn: "54321",
            Author: "Jane",
            Publisher: "New Pub",
            Edition: "2nd",
            WrittenLanguage: LanguageEnum.FR
        );

        // Act
        var result = await _controller.EditBook(1, bookUpdateDto);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("An update without content should return a NoContentResult.");

        var updatedBook = await _context.Books.FindAsync((long)1);
        updatedBook.Should().NotBeNull("The updated book should exist in the database");

        updatedBook.Title.Should().Be("New Book Title");
        updatedBook.Description.Should().Be("New description for the book");
        updatedBook.Price.Should().Be(20m);
        updatedBook.Author.Should().Be("Jane");
        updatedBook.ISBN.Should().Be("54321");
        updatedBook.Edition.Should().Be("2nd");
    }

    [Fact]
    public async Task EditBook_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var bookCategory = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(bookCategory);
        await _context.SaveChangesAsync();

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };

        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var bookUpdateDto = new BookCreateDto(
            Title: "", // Invalid
            Description: "New description for the book",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            CategoryId: 1,
            Isbn: "54321",
            Author: "Jane",
            Publisher: "New Pub",
            Edition: "2nd",
            WrittenLanguage: LanguageEnum.FR
        );

        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.EditBook(1, bookUpdateDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task EditBook_ReturnsNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var bookUpdateDto = new BookCreateDto(
            Title: "New Book Title",
            Description: "New description for the book",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW,
            CategoryId: 1,
            Isbn: "54321",
            Author: "Jane",
            Publisher: "New Pub",
            Edition: "2nd",
            WrittenLanguage: LanguageEnum.FR
        );

        // Act
        var result = await _controller.EditBook(999, bookUpdateDto); // Non-existing ID

        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for EditProduct
    [Fact]
    public async Task EditProduct_ReturnsUpdatedProduct_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        await _context.SaveChangesAsync();

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };

        var advert = new PhysicalItem
        {
            AdvertId = 1,
            Title = "Guitare",
            Description = "Guitare",
            Price = 10,
            UserId = existingUser.Id,
            User = existingUser,
            Status = AdvertStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            NotificationDate = DateTime.UtcNow,
            Condition = Condition.NEW,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var ProductUpdateDto = new ProductCreateDto(
            Title: "Guitare électrique",
            Description: "Guitare électrique",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW
        );

        // Act
        var result = await _controller.EditProduct(1, ProductUpdateDto);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("An update without content should return a NoContentResult.");

        var updatedProduct = await _context.Products.FindAsync((long)1);
        updatedProduct.Should().NotBeNull("The updated product should exist in the database");

        updatedProduct.Title.Should().Be("Guitare électrique");
        updatedProduct.Description.Should().Be("Guitare électrique");
        updatedProduct.Price.Should().Be(20m);
    }

    [Fact]
    public async Task EditProduct_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        await _context.SaveChangesAsync();

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };

        var advert = new PhysicalItem
        {
            AdvertId = 1,
            Title = "Product Title",
            Description = "Product Description",
            Price = 10,
            UserId = existingUser.Id,
            User = existingUser,
            Status = AdvertStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            NotificationDate = DateTime.UtcNow,
            Condition = Condition.NEW,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var productUpdateDto = new ProductCreateDto(
            Title: "", // Invalid
            Description: "New description for the product",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW
        );

        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.EditProduct(1, productUpdateDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task EditProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var productUpdateDto = new ProductCreateDto(
            Title: "New Product Title",
            Description: "New description for the product",
            Price: 20m,
            UserId: existingUser.Id,
            Images: new Picture[] {
                new Picture { Label = "http://example.com/newpic1.jpg" },
                new Picture { Label = "http://example.com/newpic2.jpg" }
            },
            Condition: Condition.NEW
        );

        // Act
        var result = await _controller.EditProduct(999, productUpdateDto); // Non-existing ID

        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for EditService
    [Fact]
    public async Task EditService_ReturnsUpdatedService_WhenDataIsValid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var subject = new Subject { SubjectId = 1, Name = "Mathématiques", Code = "Maths" };
        var subject2 = new Subject { SubjectId = 2, Name = "Français", Code = "Français" };
        var grade = new SchoolGrade { SchoolGradeId = 1, Name = "Terminale", Code = "Lycée" };

        _context.Set<Subject>().Add(subject);
        _context.Set<SchoolGrade>().Add(grade);
        await _context.SaveChangesAsync();

        var advert = new TutoringAdvert
        {
            AdvertId = 1,
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
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var serviceUpdateDto = new ServiceCreateDto(
            Title: "Lesson de français",
            Description: "Cours de français pour lycéens",
            Price: 40m,
            UserId: existingUser.Id,
            SubjectId: 2,
            SchoolLevelId: 1,
            TeachingLanguage: LanguageEnum.FR,
            SpecificStudyLevel: "Diplôme en Langue Française"

        );

        // Act
        var result = await _controller.EditService(1, serviceUpdateDto);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("An update without content should return a NoContentResult.");

        var updatedService = await _context.Services.FindAsync((long)1);
        updatedService.Should().NotBeNull("The updated service should exist in the database");

        updatedService.Title.Should().Be("Lesson de français");
        updatedService.Description.Should().Be("Cours de français pour lycéens");
        updatedService.Price.Should().Be(40m);
        updatedService.SubjectId.Should().Be(2);
    }

    [Fact]
    public async Task EditService_ReturnsBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var subject = new Subject { SubjectId = 1, Name = "Mathématiques", Code = "Maths" };
        var grade = new SchoolGrade { SchoolGradeId = 1, Name = "Terminale", Code = "Lycée" };

        _context.Set<Subject>().Add(subject);
        _context.Set<SchoolGrade>().Add(grade);
        var advert = new TutoringAdvert
        {
            AdvertId = 1,
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
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        var serviceUpdateDto = new ServiceCreateDto(
            Title: "", // Invalid
            Description: "Cours de français pour lycéens",
            Price: 40m,
            UserId: existingUser.Id,
            SubjectId: 1,
            SchoolLevelId: 1,
            TeachingLanguage: LanguageEnum.FR,
            SpecificStudyLevel: "Diplôme en Langue Française"

        );

        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.EditService(1, serviceUpdateDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("The controller should have returned a BadRequest result.");

        var errors = badRequestResult!.Value as SerializableError;
        errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task EditService_ReturnsNotFound_WhenServiceDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var serviceUpdateDto = new ServiceCreateDto(
            Title: "Lesson de français",
            Description: "Cours de français pour lycéens",
            Price: 40m,
            UserId: existingUser.Id,
            SubjectId: 1,
            SchoolLevelId: 1,
            TeachingLanguage: LanguageEnum.FR,
            SpecificStudyLevel: "Diplôme en Langue Française"

        );

        // Act
        var result = await _controller.EditService(999, serviceUpdateDto); // Non-existing ID

        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for UpdateAdvertStatus
    [Fact]
    public async Task UpdateAdvertStatus_ReturnsNoContent_WhenStatusIsUpdated()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var grade = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(grade);

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateAdvertStatus(1, AdvertStatus.SOLD);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("An update without content should return a NoContentResult.");

        var updatedBook = await _context.Books.FindAsync((long)1);
        updatedBook.Should().NotBeNull("The updated book should exist in the database");
        updatedBook.Status.Should().Be(AdvertStatus.SOLD);
    }

    [Fact]
    public async Task UpdateAdvertStatus_ReturnsNotFound_WhenAdvertDoesNotExist()
    {
        // Arrange
        // None

        // Act
        var result = await _controller.UpdateAdvertStatus(999, AdvertStatus.SOLD); // Non-existing ID
        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for DeleteAdvert
    [Fact]
    public async Task DeleteAdvert_ReturnsNoContent_WhenAdvertIsDeleted()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);

        var grade = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(grade);

        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteAdvert(1);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("A successful deletion should return a NoContentResult.");

        var deletedAdvert = await _context.Adverts.FindAsync((long)1);
        deletedAdvert.Should().BeNull("The deleted advert should not exist in the database");
    }

    [Fact]
    public async Task DeleteAdvert_ReturnsNotFound_WhenAdvertDoesNotExist()
    {
        // Arrange
        // None

        // Act
        var result = await _controller.DeleteAdvert(999); // Non-existing ID

        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }
    #endregion

    #region Tests for RemoveAdvertImage
    [Fact]
    public async Task RemoveAdvertImage_ReturnsNoContent_WhenImageIsRemoved()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var grade = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(grade);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveAdvertImages(1, ["http://example.com/pic1.jpg"]); // Remove first image

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull("A successful image removal should return a NoContentResult.");
        var updatedAdvert = await _context.Books.Include(b => b.Pictures).FirstOrDefaultAsync(b => b.AdvertId == 1);
        updatedAdvert.Should().NotBeNull("The advert should exist in the database");
        updatedAdvert.Pictures.Should().HaveCount(1, "One image should have been removed");
        updatedAdvert.Pictures.First().PictureId.Should().Be(2, "The remaining image should be the second one");
    }

    [Fact]
    public async Task RemoveAdvertImage_ReturnsNotFound_WhenAdvertDoesNotExist()
    {
        // Arrange
        // None

        // Act
        var result = await _controller.RemoveAdvertImages(999, ["http://example.com/pic1.jpg"]); // Non-existing ID

        // Assert
        var notFoundResult = result as NotFoundResult;
        notFoundResult.Should().NotBeNull("The controller did not return a NotFoundResult");
    }

    [Fact]
    public async Task RemoveAdvertImage_ReturnsNotFound_WhenPictureDoesNotExist()
    {
        // Arrange
        var existingUser = new User { Id = "guid-123", UserName = "john_doe", FirstName = "John", LastName = "Doe" };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(existingUser);
        var grade = new BookCategory { BookCategoryId = 1, Name = "Mathématiques", Description = "Livres de mathématiques" };
        _context.Set<BookCategory>().Add(grade);
        List<Picture> pictures = new List<Picture>
        {
            new Picture { PictureId = 1, Label = "http://example.com/pic1.jpg", AdvertId = 1 },
            new Picture { PictureId = 2, Label = "http://example.com/pic2.jpg", AdvertId = 1 }
        };
        var advert = new Book
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
            WrittenLanguage = LanguageEnum.FR,
            BookCategoryId = 1,
            Pictures = pictures
        };
        _context.Adverts.Add(advert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveAdvertImages(1, ["http://example.com/nonexistent.jpg"]); // Bad picture URL

        // Assert
        var notFoundResult = result as BadRequestObjectResult;
        notFoundResult.Should().NotBeNull("The controller did not return a BadRequestObjectResult");
    }
    #endregion
}