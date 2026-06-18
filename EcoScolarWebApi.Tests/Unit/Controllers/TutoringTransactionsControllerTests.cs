using Xunit;
using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.DTOs.Tutoring;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class TutoringTransactionsControllerTests
{
    private readonly ITutoringTransactionService _tutoringTransactionServiceMock;
    private readonly UserManager<User> _userManagerMock;
    private readonly TutoringTransactionsController _controller;

    public TutoringTransactionsControllerTests()
    {
        _tutoringTransactionServiceMock = Substitute.For<ITutoringTransactionService>();

        var store = Substitute.For<IUserStore<User>>();
        _userManagerMock = Substitute.For<UserManager<User>>(store, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new TutoringTransactionsController(_tutoringTransactionServiceMock, _userManagerMock);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupUser(string userId)
    {
        var user = new User { Id = userId };
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);
    }

    [Fact]
    public async Task Accept_UserNotFound_ReturnsUnauthorized()
    {
        _userManagerMock.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((User)null!);

        var result = await _controller.Accept(1);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Accept_ServiceReturnsSuccess_ReturnsOk()
    {
        SetupUser("user-id");
        _tutoringTransactionServiceMock.AcceptAsync(1, "user-id").Returns(Result.Success());

        var result = await _controller.Accept(1);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Refuse_ServiceReturnsForbidden_ReturnsForbid()
    {
        SetupUser("user-id");
        _tutoringTransactionServiceMock.RefuseAsync(1, "user-id").Returns(Result.Failure("Forbidden", ErrorType.Forbidden));

        var result = await _controller.Refuse(1);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Confirm_ServiceReturnsNotFound_ReturnsNotFound()
    {
        SetupUser("user-id");
        _tutoringTransactionServiceMock.ConfirmAsync(1, "user-id").Returns(Result.Failure("Not Found", ErrorType.NotFound));

        var result = await _controller.Confirm(1);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkRendered_ServiceReturnsBadRequest_ReturnsBadRequest()
    {
        SetupUser("user-id");
        _tutoringTransactionServiceMock.MarkRenderedAsync(1, "user-id").Returns(Result.Failure("Bad Request", ErrorType.BadRequest));

        var result = await _controller.MarkRendered(1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTutorContact_Success_ReturnsOkWithData()
    {
        SetupUser("user-id");
        var contactDto = new TutorContactDto("Tutor Name", "123456789", "tutor@example.com");
        _tutoringTransactionServiceMock.GetTutorContactAsync(1, "user-id").Returns(Result<TutorContactDto>.Success(contactDto));

        var result = await _controller.GetTutorContact(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(contactDto);
    }

    [Fact]
    public async Task GetTutorContact_Failure_ReturnsAppropriateError()
    {
        SetupUser("user-id");
        _tutoringTransactionServiceMock.GetTutorContactAsync(1, "user-id").Returns(Result<TutorContactDto>.Failure("Nope", ErrorType.Forbidden));

        var result = await _controller.GetTutorContact(1);

        result.Should().BeOfType<ForbidResult>();
    }
}
