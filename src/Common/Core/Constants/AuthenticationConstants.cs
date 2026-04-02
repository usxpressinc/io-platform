namespace IO.Core.Constants;

/// <summary>
/// Constants for authentication-related configuration and headers
/// </summary>
public static class AuthenticationConstants
{
    // HTTP Headers
    public const string AuthorizationHeader = "Authorization";
    public const string XAuthTokenHeader = "X-Auth-Token";
    public const string BearerScheme = "Bearer";
    
    // Environment Variables
    public const string ApiToken = "AUTH__API_TOKEN";
    public const string ValidationEndpoint = "AUTH__VALIDATION_ENDPOINT";
    public const string ValidationToken = "AUTH__VALIDATION_TOKEN";
    public const string JwtSecret = "AUTH__JWT_SECRET";
    public const string ClientSecret = "AUTH__CLIENT_SECRET";
    
    // MSAL Environment Variables
    public const string ClientId = "AUTH__CLIENT_ID";
    public const string Authority = "AUTH__AUTHORITY";
    public const string TenantId = "AUTH__TENANT_ID";
    public const string CertificatePath = "AUTH__CERTIFICATE_PATH";
    public const string CertificatePassword = "AUTH__CERTIFICATE_PASSWORD";
    
    // TokenFactory Environment Variables
    public const string TokenFactorySecret = "TOKEN_FACTORY__SECRET";
    public const string TokenFactoryMasterToken = "TOKEN_FACTORY__MASTER_TOKEN";
    public const string TokenFactoryDefaultLifetime = "TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS";
    
    // Error Codes
    public const string MissingTokenError = "missing_token";
    public const string InvalidTokenError = "invalid_token";
    public const string ExpiredTokenError = "expired_token";
    public const string InsufficientScopeError = "insufficient_scope";
    
    // Error Messages
    public const string MissingTokenMessage = "X-Auth token is required";
    public const string InvalidTokenMessage = "X-Auth token is invalid or expired";
    public const string ExpiredTokenMessage = "X-Auth token has expired";
    public const string InsufficientScopeMessage = "Insufficient scope for this operation";
    
    // Token Validation
    public const string DefaultTokenValidationTimeout = "30";
    public const string DefaultUserAgent = "IO-Proxy/1.0";
    
    // Context Items
    public const string XAuthTokenContextKey = "XAuthToken";
    public const string UserIdContextKey = "UserId";
    public const string ScopesContextKey = "Scopes";
    
    // Skip Authentication Paths
    public static readonly string[] SkipAuthenticationPaths =
    [
        "/health",
        "/ready", 
        "/swagger",
        "/openapi",
        "/swagger.json",
        "/swagger.yaml",
    ];
}

/// <summary>
/// Authentication scopes for different operations
/// </summary>
public static class AuthenticationScopes
{
    public const string ProxyRead = "io-proxy-reader";
    public const string ProxyWrite = "io-proxy-writer";
    public const string CommonRead = "io-common-reader";
    public const string CommonWrite = "io-common-writer";
    public const string CassRead = "io-cass-reader";
    public const string CassWrite = "io-cass-writer";
    public const string ElsaRead = "io-elsa-reader";
    public const string ElsaWrite = "io-elsa-writer";
    public const string LarryRead = "io-larry-reader";
    public const string LarryWrite = "io-larry-writer";
    public const string LeaRead = "io-lea-reader";
    public const string LeaWrite = "io-lea-writer";
}
