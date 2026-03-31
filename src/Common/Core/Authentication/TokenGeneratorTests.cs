using Xunit;
using System.Text.Json;

namespace IO.Core.Authentication.Tests;

/// <summary>
/// Tests for the TokenGenerator utility
/// </summary>
public class TokenGeneratorTests
{
    [Fact]
    public void GenerateSimpleToken_WithScopes_ReturnsValidToken()
    {
        // Arrange
        var scopes = new[] { "common", "clara" };
        
        // Act
        var token = TokenGenerator.GenerateSimpleToken(scopes);
        
        // Assert
        Assert.NotNull(token);
        
        // Validate the token
        var extractedScopes = TokenGenerator.ValidateSimpleToken(token);
        Assert.Equal(scopes, extractedScopes);
    }

    [Fact]
    public void GenerateJWTToken_WithScopes_ReturnsValidToken()
    {
        // Arrange
        var scopes = new[] { "common", "clara" };
        var clientId = "test-client";
        var userId = "test-user";
        
        // Act
        var token = TokenGenerator.GenerateJWTToken(scopes, clientId, userId, TimeSpan.FromHours(1));
        
        // Assert
        Assert.NotNull(token);
        Assert.StartsWith("eyJ", token);
    }

    [Fact]
    public void GenerateHappyRobotToken_Simple_ReturnsTokenWithAllScopes()
    {
        // Act
        var token = TokenGenerator.GenerateHappyRobotToken("simple");
        
        // Assert
        Assert.NotNull(token);
        
        var scopes = TokenGenerator.ValidateSimpleToken(token);
        Assert.Contains("common", scopes);
        Assert.Contains("clara", scopes);
        Assert.Contains("elsa", scopes);
        Assert.Contains("larry", scopes);
        Assert.Contains("lea", scopes);
    }

    [Fact]
    public void GenerateServiceToken_Common_ReturnsTokenWithCommonScopes()
    {
        // Act
        var token = TokenGenerator.GenerateServiceToken("common", "simple");
        
        // Assert
        Assert.NotNull(token);
        
        var scopes = TokenGenerator.ValidateSimpleToken(token);
        Assert.Equal(new[] { "common-read", "common-write" }, scopes);
    }

    [Fact]
    public void GenerateServiceToken_Cass_ReturnsTokenWithCassScopes()
    {
        // Act
        var = TokenGenerator.GenerateServiceToken("cass", "simple");
        
        // Assert
        Assert.NotNull(token);
        
        var scopes = TokenGenerator.ValidateSimpleToken(token);
        Assert.Equal(new[] { "io-cass-reader", "io-cass-writer" }, scopes);
    }

    [Fact]
    public void GenerateCurlCommand_ReturnsValidCommand()
    {
        // Arrange
        var token = "test-token";
        var endpoint = "https://api.io.proxy.usxpress.io/api/common/test";
        
        // Act
        var curl = TokenGenerator.GenerateCurlCommand(token, endpoint, "POST", new { test = "data" });
        
        // Assert
        Assert.Contains("curl -X POST", curl);
        Assert.Contains($"-H \"Authorization: Bearer {token}\"", curl);
        Assert.Contains("-H \"Content-Type: application/json\"", curl);
        Assert.Contains("-d '{\"test\\\":\\\"data\"}'", curl);
        Assert.Contains(endpoint, curl);
    }

    [Fact]
    public void GenerateServiceTestCommands_ReturnsAllServiceCommands()
    {
        // Arrange
        var baseUrl = "https://api.io.proxy.usxpress.io";
        
        // Act
        var commands = TokenGenerator.GenerateServiceTestCommands(baseUrl, "simple");
        
        // Assert
        Assert.Equal(5, commands.Count);
        Assert.True(commands.ContainsKey("common"));
        Assert.True(commands.ContainsKey("cass"));
        Assert.True(commands.ContainsKey("elsa"));
        Assert.True(commands.ContainsKey("larry"));
        Assert.True(commands.ContainsKey("lea"));
        
        // Verify each command contains required elements
        foreach (var command in commands.Values)
        {
            Assert.Contains("curl -X POST", command);
            Assert.Contains("Authorization: Bearer", command);
        }
    }

    [Fact]
    public void ValidateSimpleToken_ExpiredToken_ReturnsEmptyArray()
    {
        // Arrange
        var scopes = new[] { "common" };
        var expiredToken = TokenGenerator.GenerateSimpleToken(scopes, DateTime.UtcNow.AddHours(-1));
        
        // Act
        var extractedScopes = TokenGenerator.ValidateSimpleToken(expiredToken);
        
        // Assert
        Assert.Empty(extractedScopes);
    }
}

/// <summary>
/// Integration tests demonstrating HappyRobot token usage
/// </summary>
public class HappyRobotIntegrationTests
{
    [Fact]
    public void HappyRobot_FullFlow_DemonstratesTokenUsage()
    {
        // This would be an integration test showing the full flow:
        // 1. Generate token with all scopes
        // 2. Make authenticated request to each service
        // 3. Verify all requests succeed
        
        // Generate tokens for testing
        var allScopesToken = TokenGenerator.GenerateHappyRobotToken("simple");
        var commonToken = TokenGenerator.GenerateServiceToken("common", "simple");
        var cassToken = TokenGenerator.GenerateServiceToken("cass", "simple");
        
        // Test commands for manual testing
        var commands = TokenGenerator.GenerateServiceTestCommands("https://api.io.proxy.usxpress.io", "simple");
        
        // Example: Test common service
        var commonCurl = commands["common"];
        Assert.Contains("Authorization: Bearer " + commonToken, commonCurl);
        
        // These would be the actual commands HappyRobot would use:
        // curl -X POST -H "Authorization: Bearer <common-token>" -H "Content-Type: application/json" -d '{"test": "data"}' https://api.io.proxy.usxpress.io/api/common/email
        // curl -X POST -H "Authorization: Bearer <cass-token>" -H "Content-Type: application/json" -d '{"dot_number": "12345"}' https://api.io.proxy.usxpress.io/api/clara/carriers/valid
        // etc.
    }
}
