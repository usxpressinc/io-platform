using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IO.Core.Authentication.Tests;

/// <summary>
/// Unit tests for TokenFactory
/// </summary>
public class TokenFactoryTests
{
    private readonly TokenFactory _tokenFactory;
    private readonly TokenFactoryOptions _options;

    public TokenFactoryTests()
    {
        _options = new TokenFactoryOptions
        {
            FactorySecret = "test-factory-secret",
            MasterTokenSecret = "test-master-secret"
        };

        var loggerMock = new Mock<ILogger<TokenFactory>>();
        _tokenFactory = new TokenFactory(_options, loggerMock.Object);
    }

    [Fact]
    public void GenerateMasterToken_CreatesValidToken()
    {
        // Act
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));

        // Assert
        Assert.NotNull(masterToken);
        
        // Validate the master token
        var tokenBytes = Convert.FromBase64String(masterToken);
        var tokenJson = System.Text.Json.JsonSerializer.Deserialize<MasterTokenData>(
            System.Text.Json.JsonSerializer.Deserialize<string>(tokenJson)!);
        
        Assert.Equal("master", tokenJson.type);
        Assert.True(tokenJson.expires_at > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public void GenerateScopedToken_WithValidMasterToken_ReturnsToken()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var scopes = new[] { "common-read", "common-write" };

        // Act
        var result = _tokenFactory.GenerateScopedToken(masterToken, scopes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scopes, result.Scopes);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.NotNull(result.TokenId);
        Assert.True(result.Token.Contains('.'));
    }

    [Fact]
    public void GenerateScopedToken_WithInvalidMasterToken_ReturnsNull()
    {
        // Arrange
        var invalidMasterToken = "invalid-token";
        var scopes = new[] { "common-read" };

        // Act
        var result = _tokenFactory.GenerateScopedToken(invalidMasterToken, scopes);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateScopedToken_WithValidToken_ReturnsTokenData()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var scopes = new[] { "common-read", "common-write" };
        var tokenResult = _tokenFactory.GenerateScopedToken(masterToken, scopes);
        
        // Act
        var tokenData = _tokenFactory.ValidateScopedToken(tokenResult!.Token);

        // Assert
        Assert.NotNull(tokenData);
        Assert.Equal("scoped", tokenData.type);
        Assert.Equal(scopes, tokenData.scopes);
        Assert.Equal(tokenResult.TokenId, tokenData.token_id);
    }

    [Fact]
    public void ValidateScopedToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token";

        // Act
        var tokenData = _tokenFactory.ValidateScopedToken(invalidToken);

        // Assert
        Assert.Null(tokenData);
    }

    [Fact]
    public void ValidateScopedToken_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var scopes = new[] { "common-read" };
        var tokenResult = _tokenFactory.GenerateScopedToken(masterToken, scopes, TimeSpan.FromSeconds(-1)); // Expired
        
        // Act
        var tokenData = _tokenFactory.ValidateScopedToken(tokenResult!.Token);

        // Assert
        Assert.Null(tokenData);
    }

    [Fact]
    public void GenerateAllServiceTokens_CreatesTokensForAllServices()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));

        // Act
        var tokens = _tokenFactory.GenerateAllServiceTokens(masterToken, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(5, tokens.Count);
        Assert.True(tokens.ContainsKey("common"));
        Assert.True(tokens.ContainsKey("cass"));
        Assert.True(tokens.ContainsKey("elsa"));
        Assert.True(tokens.ContainsKey("larry"));
        Assert.True(tokens.ContainsKey("lea"));

        // Verify each token has correct scopes
        Assert.Equal(new[] { "io-common-reader", "io-common-writer" }, tokens["common"].Scopes);
        Assert.Equal(new[] { "io-cass-reader", "io-cass-writer" }, tokens["cass"].Scopes);
        Assert.Equal(new[] { "io-elsa-reader", "io-elsa-writer" }, tokens["elsa"].Scopes);
        Assert.Equal(new[] { "io-larry-reader", "io-larry-writer" }, tokens["larry"].Scopes);
        Assert.Equal(new[] { "io-lea-reader", "io-lea-writer" }, tokens["lea"].Scopes);
    }

    [Fact]
    public void GenerateScopedToken_WithRequesterId_IncludesInTokenData()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var scopes = new[] { "common-read" };
        var requesterId = "happy-robot-test";

        // Act
        var result = _tokenFactory.GenerateScopedToken(masterToken, scopes, null, requesterId);
        var tokenData = _tokenFactory.ValidateScopedToken(result!.Token);

        // Assert
        Assert.Equal(requesterId, tokenData.requester_id);
    }

    [Fact]
    public void GenerateScopedToken_WithDifferentValidityTimes_SetsCorrectExpiry()
    {
        // Arrange
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var scopes = new[] { "common-read" };
        var validFor = TimeSpan.FromMinutes(30);

        // Act
        var result = _tokenFactory.GenerateScopedToken(masterToken, scopes, validFor);

        // Assert
        var expectedExpiry = DateTime.UtcNow.Add(validFor);
        var timeDifference = Math.Abs((result!.ExpiresAt - expectedExpiry).TotalSeconds);
        Assert.True(timeDifference < 5); // Allow 5 seconds tolerance
    }
}

/// <summary>
/// Integration tests demonstrating HappyRobot token usage with TokenFactory
/// </summary>
public class TokenFactoryIntegrationTests
{
    [Fact]
    public void HappyRobot_FullFlow_DemonstratesZeroTrustTokenGeneration()
    {
        // This demonstrates the complete zero-trust flow:
        // 1. Generate master token (one-time setup)
        // 2. Use master token to generate scoped tokens at runtime
        // 3. Validate scoped tokens for API access
        // 4. Master token cannot access API endpoints

        // Step 1: Generate master token (this would be done once and stored securely)
        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(365));
        var options = new TokenFactoryOptions
        {
            FactorySecret = "test-factory-secret",
            MasterTokenSecret = "test-master-secret"
        };
        var loggerMock = new Mock<ILogger<TokenFactory>>();
        var tokenFactory = new TokenFactory(options, loggerMock.Object);

        // Step 2: Generate scoped tokens for HappyRobot at runtime
        var commonToken = tokenFactory.GenerateScopedToken(
            masterToken, 
            new[] { "io-common-reader", "io-common-write" }, 
            TimeSpan.FromHours(1),
            "happy-robot-common");

        var cassToken = tokenFactory.GenerateScopedToken(
            masterToken, 
            new[] { "io-cass-reader", "io-cass-writer" }, 
            TimeSpan.FromHours(1),
            "happy-robot-cass");

        // Step 3: Validate the scoped tokens
        var commonTokenData = tokenFactory.ValidateScopedToken(commonToken!.Token);
        var cassTokenData = tokenFactory.ValidateScopedToken(cassToken!.Token);

        // Step 4: Verify tokens are valid and have correct scopes
        Assert.NotNull(commonTokenData);
        Assert.NotNull(cassTokenData);
        Assert.Equal("happy-robot-common", commonTokenData.requester_id);
        Assert.Equal("happy-robot-cass", cassTokenData.requester_id);
        Assert.Contains("io-common-reader", commonTokenData.scopes);
        Assert.Contains("io-cass-reader", cassTokenData.scopes);

        // Step 5: Generate all service tokens for complete test suite
        var allTokens = tokenFactory.GenerateAllServiceTokens(masterToken, TimeSpan.FromHours(2));
        Assert.Equal(5, allTokens.Count);

        // These tokens can now be used for API testing:
        // curl -H "Authorization: Bearer <common-token>" https://api.io.proxy.usxpress.io/api/common/email
        // curl -H "Authorization: Bearer <cass-token>" https://api.io.proxy.usxpress.io/api/clara/carriers/valid
        // etc.

        // The master token itself cannot be used for API access - only for generating scoped tokens
    }

    [Fact]
    public void MasterToken_CannotBeUsedForAPI_DemonstratesSecurity()
    {
        // This test demonstrates that the master token cannot be used to access API endpoints
        // It can only be used to generate scoped tokens

        var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(1));
        var options = new TokenFactoryOptions
        {
            FactorySecret = "test-factory-secret",
            MasterTokenSecret = "test-master-secret"
        };
        var loggerMock = new Mock<ILogger<TokenFactory>>();
        var tokenFactory = new TokenFactory(options, loggerMock.Object);

        // Try to validate master token as a scoped token
        var tokenData = tokenFactory.ValidateScopedToken(masterToken);

        // The master token should not validate as a scoped token
        Assert.Null(tokenData);

        // But it can be used to generate scoped tokens
        var scopedToken = tokenFactory.GenerateScopedToken(
            masterToken, 
            new[] { "io-common-reader" }, 
            TimeSpan.FromHours(1));

        Assert.NotNull(scopedToken);
        Assert.NotNull(tokenFactory.ValidateScopedToken(scopedToken.Token));
    }
}
