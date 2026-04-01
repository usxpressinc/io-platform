using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace IO.Platform.Common.Models;

/// <summary>
/// Represents X-Auth token information for zero-trust authentication
/// Used by TokenFactory for runtime token generation and validation
/// </summary>
[BsonIgnoreExtraElements]
public class XAuthToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("token_id")]
    public string TokenId { get; set; } = string.Empty;

    [Required]
    [BsonElement("token_value")]
    public string TokenValue { get; set; } = string.Empty;

    [Required]
    [BsonElement("requester_id")]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    [BsonElement("scopes")]
    public List<string> Scopes { get; set; } = new();

    [BsonElement("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("issued_at")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("issuer")]
    public string Issuer { get; set; } = "io-platform";

    [BsonElement("audience")]
    public string Audience { get; set; } = "io-platform-services";

    [BsonElement("token_type")]
    public TokenType TokenType { get; set; } = TokenType.Scoped;

    [BsonElement("status")]
    public TokenStatus Status { get; set; } = TokenStatus.Active;

    [BsonElement("usage_count")]
    public int UsageCount { get; set; } = 0;

    [BsonElement("last_used")]
    public DateTime? LastUsed { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of X-Auth tokens
/// </summary>
public enum TokenType
{
    Master,
    Scoped,
    Service,
    User
}

/// <summary>
/// Token status enumeration
/// </summary>
public enum TokenStatus
{
    Active,
    Expired,
    Revoked,
    Suspended
}

/// <summary>
/// Token generation request
/// </summary>
public class TokenGenerationRequest
{
    [Required]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    public List<string> Scopes { get; set; } = new();

    public TimeSpan? Expiration { get; set; }

    public TokenType TokenType { get; set; } = TokenType.Scoped;

    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? TokenId { get; set; }
    public string? RequesterId { get; set; }
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TokenType TokenType { get; set; }
}

/// <summary>
/// Token revocation request
/// </summary>
public class TokenRevocationRequest
{
    [Required]
    public string TokenId { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public bool RevokeAllForRequester { get; set; } = false;
}

/// <summary>
/// Token usage audit log
/// </summary>
[BsonIgnoreExtraElements]
public class TokenUsageLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("token_id")]
    public string TokenId { get; set; } = string.Empty;

    [Required]
    [BsonElement("requester_id")]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    [BsonElement("service_name")]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [BsonElement("http_method")]
    public string HttpMethod { get; set; } = string.Empty;

    [Required]
    [BsonElement("ip_address")]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [BsonElement("user_agent")]
    public string UserAgent { get; set; } = string.Empty;

    [BsonElement("response_status")]
    public int? ResponseStatus { get; set; }

    [BsonElement("response_time_ms")]
    public int? ResponseTimeMs { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("success")]
    public bool Success { get; set; }

    [BsonElement("error_message")]
    public string? ErrorMessage { get; set; }
}
