using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using IO.Core.Constants;

namespace IO.Core.Authentication;

/// <summary>
/// Utility for generating X-Auth tokens with specified scopes for testing
/// </summary>
public static class TokenGenerator
{
    /// <summary>
    /// Generates a simple token (like Python version) with predefined scopes
    /// </summary>
    /// <param name="scopes">The scopes to include in the token</param>
    /// <param name="validUntil">Optional expiration time (defaults to 1 year from now)</param>
    /// <returns>A simple token string</returns>
    public static string GenerateSimpleToken(string[] scopes, DateTime? validUntil = null)
    {
        // Create a simple token format: base64(json(scopes + expiry))
        var expiry = validUntil ?? DateTime.UtcNow.AddYears(1);
        var tokenData = new
        {
            scopes = scopes,
            expires_at = expiry.ToUnixTimeSeconds(),
            type = "simple"
        };
        
        var tokenJson = JsonSerializer.Serialize(tokenData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson));
    }

    /// <summary>
    /// Generates a JWT token with specified scopes using MSAL-like format
    /// </summary>
    /// <param name="scopes">The scopes to include in the token</param>
    /// <param name="clientId">The client ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="validFor">How long the token is valid for</param>
    /// <param name="issuer">Optional issuer override</param>
    /// <param name="audience">Optional audience override</param>
    /// <returns>A JWT token string</returns>
    public static string GenerateJWTToken(
        string[] scopes, 
        string clientId, 
        string userId, 
        TimeSpan validFor,
        string? issuer = null,
        string? audience = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASecretKeyForTokenGeneration"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            new Claim("client_id", clientId),
            new Claim("scopes", string.Join(" ", scopes)),
            new Claim("token_type", "jwt")
        };

        if (!string.IsNullOrEmpty(issuer))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
        }

        if (!string.IsNullOrEmpty(audience))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = userId,
            Expires = DateTime.UtcNow.Add(validFor),
            Issuer = issuer ?? "io-platform",
            Audience = audience ?? "io-proxy",
            SigningCredentials = creds,
            Claims = claims
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a token for HappyRobot testing with all IO platform scopes
    /// </summary>
    /// <param name="tokenType">Type of token to generate ("simple" or "jwt")</param>
    /// <returns>A token with all required scopes</returns>
    public static string GenerateHappyRobotToken(string tokenType = "simple")
    {
        var allScopes = new[] 
        { 
            AuthenticationScopes.ProxyRead, 
            AuthenticationScopes.ProxyWrite,
            AuthenticationScopes.CommonRead, 
            AuthenticationScopes.CommonWrite,
            AuthenticationScopes.CassRead, 
            AuthenticationScopes.CassWrite,
            AuthenticationScopes.ElsaRead, 
            AuthenticationScopes.ElsaWrite,
            AuthenticationScopes.LarryRead, 
            AuthenticationScopes.LarryWrite,
            AuthenticationScopes.LeaRead, 
            AuthenticationScopes.LeaWrite
        };

        return tokenType.ToLower() switch
        {
            "jwt" => GenerateJWTToken(
                allScopes, 
                "happy-robot-client", 
                "happy-robot-user", 
                TimeSpan.FromHours(1)),
            _ => GenerateSimpleToken(allScopes)
        };
    }

    /// <summary>
    /// Generates a token with specific service scopes for testing
    /// </summary>
    /// <param name="service">The service name (common, cass, elsa, larry, lea)</param>
    /// <param name="tokenType">Type of token to generate ("simple" or "jwt")</param>
    /// <returns>A token with the specified service scopes</returns>
    public static string GenerateServiceToken(string service, string tokenType = "simple")
    {
        var scopes = service.ToLower() switch
        {
            "common" => new[] { AuthenticationScopes.CommonRead, AuthenticationScopes.CommonWrite },
            "cass" or "clara" => new[] { AuthenticationScopes.CassRead, AuthenticationScopes.CassWrite },
            "elsa" => new[] { AuthenticationScopes.ElsaRead, AuthenticationScopes.ElsaWrite },
            "larry" => new[] { AuthenticationScopes.LarryRead, AuthenticationScopes.LarryWrite },
            "lea" => new[] { AuthenticationScopes.LeaRead, AuthenticationScopes.LeaWrite },
            "proxy" => new[] { AuthenticationScopes.ProxyRead, AuthenticationScopes.ProxyWrite },
            _ => Array.Empty<string>()
        };

        return tokenType.ToLower() switch
        {
            "jwt" => GenerateJWTToken(
                scopes, 
                "test-client", 
                "test-user", 
                TimeSpan.FromHours(1)),
            _ => GenerateSimpleToken(scopes)
        };
    }

    /// <summary>
    /// Validates a simple token and extracts scopes
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>The scopes contained in the token, or empty array if invalid</returns>
    public static string[] ValidateSimpleToken(string token)
    {
        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<SimpleTokenData>(tokenJson);
            
            // Check if token is expired
            if (tokenData.expires_at < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return Array.Empty<string>();
            }
            
            return tokenData.scopes ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Creates a curl command for testing with the generated token
    /// </summary>
    /// <param name="token">The token to use</param>
    /// <param name="endpoint">The endpoint to test</param>
    /// <param name="method">HTTP method (GET, POST, etc.)</param>
    /// <param name="data">Optional JSON data to send</param>
    /// <returns>A curl command string</returns>
    public static string GenerateCurlCommand(
        string token, 
        string endpoint, 
        string method = "GET", 
        object? data = null)
    {
        var curl = $"curl -X {method}";
        
        // Add headers
        curl += " -H \"Authorization: Bearer {token}\"";
        curl += " -H \"Content-Type: application/json\"";
        
        // Add data if provided
        if (data != null)
        {
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
            curl += $" -d '{jsonData}'";
        }
        
        // Add URL
        curl += $" {endpoint}";
        
        return curl;
    }

    /// <summary>
    /// Creates test commands for all services with their respective tokens
    /// </summary>
    /// <param name="baseUrl">The base URL of the API</param>
    /// <param name="tokenType">Type of token to generate ("simple" or "jwt")</param>
    /// <returns>A dictionary of service names and their test commands</returns>
    public static Dictionary<string, string> GenerateServiceTestCommands(
        string baseUrl, 
        string tokenType = "simple")
    {
        var commands = new Dictionary<string, string>();
        
        var services = new[] { "common", "cass", "elsa", "larry", "lea" };
        
        foreach (var service in services)
        {
            var token = GenerateServiceToken(service, tokenType);
            var endpoint = $"{baseUrl}/api/{service}/test";
            
            commands[service] = GenerateCurlCommand(token, endpoint, "POST", new { test = "data" });
        }
        
        return commands;
    }
}

/// <summary>
/// Simple token data structure for base64 encoded tokens
/// </summary>
internal class SimpleTokenData
{
    public string[] scopes { get; set; } = Array.Empty<string>();
    public long expires_at { get; set; }
    public string type { get; set; } = "simple";
}
