using Xunit;
using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class ReportsControllerTests
{
    private readonly IAbuseReportService _reportServiceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _reportServiceMock = Substitute.For<IAbuseReportService>();
        _controller = new ReportsController(_reportServiceMock);
    }

    private void SetUser(string? userId)
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, userId != null ? "TestAuth" : null))
            }
        };
    }

    [Fact]
    public async Task CreateReport_ShouldReturnUnauthorized_WhenUserIdIsMissing()
    {
        // Arrange
        SetUser(null);
        var requestDto = new AbuseReportRequestDto
        {
            TargetAdvertId = 1,
            Reason = ReportReason.INAPPROPRIATE_ADVERT,
            Message = "This is inappropriate"
        };

        // Act
        var result = await _controller.CreateReport(requestDto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateReport_ShouldReturnCreatedAtAction_WhenValid()
    {
        // Arrange
        SetUser("user-123");
        var requestDto = new AbuseReportRequestDto
        {
            TargetAdvertId = 42,
            Reason = ReportReason.INAPPROPRIATE_ADVERT,
            Message = "Offensive content"
        };

        var responseDto = new AbuseReportResponseDto
        {
            Id = 1,
            TargetAdvertId = 42,
            ReporterUserId = "user-123",
            Reason = ReportReason.INAPPROPRIATE_ADVERT,
            Message = "Offensive content",
            Status = TicketStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        _reportServiceMock.CreateReportAsync(requestDto, "user-123").Returns(responseDto);

        // Act
        var result = await _controller.CreateReport(requestDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.CreateReport));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(1);

        var value = createdResult.Value.Should().BeOfType<AbuseReportResponseDto>().Subject;
        value.TargetAdvertId.Should().Be(42);
        value.ReporterUserId.Should().Be("user-123");
    }

    [Fact]
    public async Task CreateReport_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        SetUser("user-456");
        var requestDto = new AbuseReportRequestDto
        {
            TargetAdvertId = 100,
            TargetCommentId = 5,
            Reason = ReportReason.INAPPROPRIATE_COMMENT,
            Message = "Rude comment"
        };

        _reportServiceMock.CreateReportAsync(Arg.Any<AbuseReportRequestDto>(), Arg.Any<string>())
            .Returns(new AbuseReportResponseDto
            {
                Id = 2,
                TargetAdvertId = 100,
                TargetCommentId = 5,
                ReporterUserId = "user-456",
                Reason = ReportReason.INAPPROPRIATE_COMMENT,
                Message = "Rude comment",
                Status = TicketStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        await _controller.CreateReport(requestDto);

        // Assert
        await _reportServiceMock.Received(1).CreateReportAsync(
            Arg.Is<AbuseReportRequestDto>(r => r.TargetAdvertId == 100 && r.TargetCommentId == 5),
            "user-456"
        );
    }
}
