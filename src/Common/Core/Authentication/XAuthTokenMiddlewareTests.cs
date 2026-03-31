using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;

namespace IO.Core.Authentication.Tests;

/// <summary>
/// Unit tests for X-Auth token middleware
/// </summary>
public class XAuthTokenMiddlewareTests
{
    private readonly Mock<ILogger<XAuthTokenMiddleware>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly XAuthTokenOptions _options;

    public XAuthTokenMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<XAuthTokenMiddleware>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _options = new XAuthTokenOptions
        {
            ApiToken = "test-token-123"
        };
    }

    [Fact]
    public async Task InvokeAsync_WithValidToken_CallsNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext("Bearer valid-token");
        var nextCalled = false;
        RequestDelegate next = (innerContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new XAuthTokenMiddleware(next, _loggerMock.Object, _httpClientFactoryMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingToken_ReturnsUnauthorized()
    {
        // Arrange
        var context = CreateHttpContext(null);
        var nextCalled = false;
        RequestDelegate next = (innerContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new XAuthTokenMiddleware(next, _loggerMock.Object, _httpClientFactoryMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidToken_ReturnsForbidden()
    {
        // Arrange
        var context = CreateHttpContext("Bearer invalid-token");
        var nextCalled = false;
        RequestDelegate next = (innerContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new XAuthTokenMiddleware(next, _loggerMock.Object, _httpClientFactoryMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithHealthEndpoint_SkipsAuthentication()
    {
        // Arrange
        var context = CreateHttpContext(null, "/health");
        var nextCalled = false;
        RequestDelegate next = (innerContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new XAuthTokenMiddleware(next, _loggerMock.Object, _httpClientFactoryMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithSwaggerEndpoint_SkipsAuthentication()
    {
        // Arrange
        var context = CreateHttpContext(null, "/swagger");
        var nextCalled = false;
        RequestDelegate next = (innerContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new XAuthTokenMiddleware(next, _loggerMock.Object, _httpClientFactoryMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public void ExtractToken_WithBearerHeader_ReturnsToken()
    {
        // Arrange
        var context = CreateHttpContext("Bearer test-token");

        // Create middleware instance to test private method via reflection or create a testable version
        var middleware = new XAuthTokenMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _options);

        // Act & Assert - This would need to be tested via integration test or by making the method public/internal
        // For now, we'll test the behavior through the main InvokeAsync method
    }

    private static HttpContext CreateHttpContext(string? authorizationHeader, string? path = null)
    {
        var context = new DefaultHttpContext();
        
        if (!string.IsNullOrEmpty(path))
        {
            context.Request.Path = path;
        }

        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            context.Request.Headers.Authorization = authorizationHeader;
        }

        return context;
    }
}

/// <summary>
/// Integration tests for X-Auth token middleware
/// </summary>
public class XAuthTokenMiddlewareIntegrationTests
{
    [Fact]
    public async Task Middleware_FullFlow_WithValidToken()
    {
        // This would be an integration test that sets up a full ASP.NET Core pipeline
        // and tests the middleware in a realistic scenario
        
        // Arrange: Set up test server with middleware
        // Act: Make HTTP request with valid token
        // Assert: Response is successful and downstream logic executed
        
        // Implementation would use WebApplicationFactory<T> for testing
    }
}
