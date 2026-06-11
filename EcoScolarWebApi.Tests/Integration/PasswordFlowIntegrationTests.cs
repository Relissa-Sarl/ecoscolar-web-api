using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

public class PasswordFlowIntegrationTests : IClassFixture<AuthInMemoryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PasswordFlowIntegrationTests(AuthInMemoryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Forgot Password Tests

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_EvenWhenEmailIsMalformed()
    {
        // Arrange
        var payload = new { email = "malformed_email_address" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgotPassword", payload);

        // Assert
        // Microsoft Identity always returns 200 OK for security reasons (User Enumeration Protection)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_WhenEmailIsValidGenericPlaceholder()
    {
        // Arrange
        var payload = new { email = "user@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgotPassword", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_ShouldReturnBadRequest_WithIdentityErrors_WhenPayloadIsInvalid()
    {
        // Arrange
        var payload = new
        {
            email = "user@example.com",
            resetCode = "invalid_or_expired_token_placeholder",
            newPassword = "123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/resetPassword", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify the response contains the native Identity error dictionary structure required by the frontend
        var jsonResult = await response.Content.ReadAsStringAsync();
        jsonResult.Should().Contain("\"errors\":");
        jsonResult.Should().Contain("InvalidToken");
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenIsEmpty()
    {
        // Arrange
        var payload = new
        {
            email = "user@example.com",
            resetCode = "",
            newPassword = "SecureGenericPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/resetPassword", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}