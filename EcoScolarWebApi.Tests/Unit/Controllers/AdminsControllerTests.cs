using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class AdminsControllerTests : IDisposable
{
    private readonly EcoscolarDbContext _context;
    private readonly UserManager<User> _userManagerMock;
    private readonly IAdminService _adminServiceMock;
    private readonly AdminsController _controller;

    public AdminsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EcoscolarDbContext(options);

        var store = Substitute.For<IUserStore<User>>();
        _userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
        _adminServiceMock = Substitute.For<IAdminService>();

        _controller = new AdminsController(_adminServiceMock, _userManagerMock, _context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")]))
                }
            }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var users = new List<UserResponse> { new UserResponse("1", "User1", "First", "Last", "test@test.ch", false, false, [], null, "2000-01-01", []) };
        _adminServiceMock.GetAllUsers(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<List<UserResponse>>.Success(users));

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsUnauthorized_WhenServiceReturnsUnauthorized()
    {
        // Arrange
        _adminServiceMock.GetAllUsers(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<List<UserResponse>>.Failure("Unauthorized access", ErrorType.Unauthorized));

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetAllSupports_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var supports = new List<SupportTicketAdminDto> { new SupportTicketAdminDto(1, "test@test.ch", "Subject", "Message", "user-1", DateTime.UtcNow, new UserAdminDto("F", "L", "N", "E"), []) };
        _adminServiceMock.GetAllSupports(Arg.Any<ClaimsPrincipal>())
            .Returns(Result<List<SupportTicketAdminDto>>.Success(supports));

        // Act
        var result = await _controller.GetAllSupports();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(supports);
    }

    [Fact]
    public async Task AddTicketMessage_ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        var id = 1;
        var request = new SupportTicketMessageRequestDto { Message = "Hello" };
        var response = new SupportTicketMessageAdminDto(10, "Hello", true, DateTime.UtcNow);
        _adminServiceMock.AddTicketMessage(Arg.Any<ClaimsPrincipal>(), id, request)
            .Returns(Result<SupportTicketMessageAdminDto>.Success(response));

        // Act
        var result = await _controller.AddTicketMessage(id, request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task BanUser_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var userId = "user-123";
        var response = new UserResponse(userId, "User1", "First", "Last", "test@test.ch", true, true, [], null, "2000-01-01", []);
        _adminServiceMock.BanUserToggle(Arg.Any<ClaimsPrincipal>(), userId)
            .Returns(Result<UserResponse>.Success(response));

        // Act
        var result = await _controller.BanUser(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task BlockAdvert_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var advertId = 100L;
        var response = new AdvertReadDto(advertId, "BOOK", "Blocked", 10m, DateTime.UtcNow, DateTime.UtcNow, AdvertStatus.BLOCKED, "seller-1", "Seller", null, "", 30);
        _adminServiceMock.BlockAdvert(Arg.Any<ClaimsPrincipal>(), advertId)
            .Returns(Result<AdvertReadDto>.Success(response));

        // Act
        var result = await _controller.BlockAdvert(advertId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }
}
