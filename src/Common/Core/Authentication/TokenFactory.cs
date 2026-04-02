using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using IO.Core.Constants;

namespace IO.Core.Authentication;

/// <summary>
/// Token factory for generating scoped tokens using a master token
/// Master token cannot access routes, only generates scoped tokens
/// </summary>
public class TokenFactory
{
    private readonly TokenFactoryOptions _options;
    private readonly ILogger<TokenFactory> _logger;

    public TokenFactory(TokenFactoryOptions options, ILogger<TokenFactory> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Generates a scoped token using the master token
    /// </summary>
    /// <param name="masterToken">The master token for authentication</param>
    /// <param name="scopes">The scopes to include in the generated token</param>
    /// <param name="validFor">How long the generated token should be valid</param>
    /// <param name="requesterId">Optional requester identifier for audit</param>
    /// <returns>A scoped token or null if master token is invalid</returns>
    public ScopedTokenResult? GenerateScopedToken(
        string masterToken, 
        string[] scopes, 
        TimeSpan? validFor = null,
        string? requesterId = null)
    {
        try
        {
            // Validate the master token first
            if (!ValidateMasterToken(masterToken))
            {
                _logger.LogWarning("Invalid master token used for token generation");
                return null;
            }

            // Generate the scoped token
            var expiry = DateTime.UtcNow.Add(validFor ?? TimeSpan.FromHours(1));
            var tokenId = Guid.NewGuid().ToString();
            
            var tokenData = new ScopedTokenData
            {
                token_id = tokenId,
                scopes = scopes,
                expires_at = ((DateTimeOffset)expiry).ToUnixTimeSeconds(),
                generated_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                generated_by = "token-factory",
                requester_id = requesterId ?? "unknown",
                type = "scoped"
            };

            // Sign the token data with the factory secret
            var tokenJson = JsonSerializer.Serialize(tokenData);
            var signature = SignToken(tokenJson);
            
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson)) + "." + signature;

            return new ScopedTokenResult
            {
                Token = token,
                TokenId = tokenId,
                Scopes = scopes,
                ExpiresAt = expiry,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating scoped token");
            return null;
        }
    }

    /// <summary>
    /// Validates a scoped token and extracts its data
    /// </summary>
    /// <param name="scopedToken">The scoped token to validate</param>
    /// <returns>The token data or null if invalid</returns>
    public ScopedTokenData? ValidateScopedToken(string scopedToken)
    {
        try
        {
            var parts = scopedToken.Split('.');
            if (parts.Length != 2)
            {
                return null;
            }

            var tokenJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var signature = parts[1];

            // Verify the signature
            var expectedSignature = SignToken(tokenJson);
            if (!string.Equals(signature, expectedSignature, StringComparison.Ordinal))
            {
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<ScopedTokenData>(tokenJson);
            
            // Check if token is expired
            if (tokenData.expires_at < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }

            return tokenData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scoped token");
            return null;
        }
    }

    /// <summary>
    /// Validates the master token
    /// </summary>
    /// <param name="masterToken">The master token to validate</param>
    /// <returns>True if the master token is valid</returns>
    private bool ValidateMasterToken(string masterToken)
    {
        try
        {
            // Master token should be a simple base64 encoded JSON with type "master"
            var tokenBytes = Convert.FromBase64String(masterToken);
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<MasterTokenData>(tokenJson);

            if (tokenData?.type != "master")
            {
                return false;
            }

            // Check if master token is expired
            if (tokenData.expires_at < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            // Verify master token signature if configured
            if (!string.IsNullOrEmpty(_options.MasterTokenSignature))
            {
                var expectedSignature = ComputeHash(tokenJson, _options.MasterTokenSecret);
                return string.Equals(tokenData.signature, expectedSignature, StringComparison.Ordinal);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Signs token data with the factory secret
    /// </summary>
    /// <param name="tokenJson">The token JSON to sign</param>
    /// <returns>The signature</returns>
    private string SignToken(string tokenJson)
    {
        return ComputeHash(tokenJson, _options.FactorySecret);
    }

    /// <summary>
    /// Computes HMAC-SHA256 hash
    /// </summary>
    /// <param name="data">The data to hash</param>
    /// <param name="secret">The secret key</param>
    /// <returns>The base64 encoded hash</returns>
    private static string ComputeHash(string data, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var message = Encoding.UTF8.GetBytes(data);
        
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(message);
        
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Generates a master token (for initial setup)
    /// </summary>
    /// <param name="validFor">How long the master token should be valid</param>
    /// <returns>A master token</returns>
    public static string GenerateMasterToken(TimeSpan validFor)
    {
        var expiry = DateTime.UtcNow.Add(validFor);
        var tokenData = new MasterTokenData
        {
            type = "master",
            expires_at = ((DateTimeOffset)expiry).ToUnixTimeSeconds(),
            generated_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            purpose = "token-generation"
        };

        var tokenJson = JsonSerializer.Serialize(tokenData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson));
    }

    /// <summary>
    /// Generates scoped tokens for all IO services for HappyRobot
    /// </summary>
    /// <param name="masterToken">The master token</param>
    /// <param name="validFor">How long the tokens should be valid</param>
    /// <returns>Dictionary of service names and their tokens</returns>
    public Dictionary<string, ScopedTokenResult> GenerateAllServiceTokens(
        string masterToken, 
        TimeSpan? validFor = null)
    {
        var tokens = new Dictionary<string, ScopedTokenResult>();
        
        var services = new[]
        {
            new { Name = "common", Scopes = new[] { AuthenticationScopes.CommonRead, AuthenticationScopes.CommonWrite } },
            new { Name = "cass", Scopes = new[] { AuthenticationScopes.CassRead, AuthenticationScopes.CassWrite } },
            new { Name = "elsa", Scopes = new[] { AuthenticationScopes.ElsaRead, AuthenticationScopes.ElsaWrite } },
            new { Name = "larry", Scopes = new[] { AuthenticationScopes.LarryRead, AuthenticationScopes.LarryWrite } },
            new { Name = "lea", Scopes = new[] { AuthenticationScopes.LeaRead, AuthenticationScopes.LeaWrite } }
        };

        foreach (var service in services)
        {
            var token = GenerateScopedToken(
                masterToken, 
                service.Scopes, 
                validFor, 
                $"happy-robot-{service.Name}");
            
            if (token != null)
            {
                tokens[service.Name] = token;
            }
        }

        return tokens;
    }
}

/// <summary>
/// Configuration options for TokenFactory
/// </summary>
public class TokenFactoryOptions
{
    public string FactorySecret { get; set; } = "default-factory-secret-change-in-production";
    public string? MasterToken { get; set; } // Read from environment variable
    public TimeSpan DefaultTokenLifetime { get; set; } = TimeSpan.FromHours(1);
    public string? MasterTokenSignature { get; set; }
    public string? MasterTokenSecret { get; set; }
}

/// <summary>
/// Result from generating a scoped token
/// </summary>
public class ScopedTokenResult
{
    public string Token { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Data structure for scoped tokens
/// </summary>
public class ScopedTokenData
{
    public string token_id { get; set; } = string.Empty;
    public string[] scopes { get; set; } = Array.Empty<string>();
    public long expires_at { get; set; }
    public long generated_at { get; set; }
    public string generated_by { get; set; } = "token-factory";
    public string requester_id { get; set; } = string.Empty;
    public string type { get; set; } = "scoped";
}

/// <summary>
/// Data structure for master tokens
/// </summary>
internal class MasterTokenData
{
    public string type { get; set; } = "master";
    public long expires_at { get; set; }
    public long generated_at { get; set; }
    public string purpose { get; set; } = string.Empty;
    public string signature { get; set; } = string.Empty; // Optional signature
}
