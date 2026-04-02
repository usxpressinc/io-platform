# Authentication Flow Documentation

**Feature**: X-Auth Token Middleware and TokenFactory  
**Version**: 1.0.0 | **Date**: 2026-03-31

## Architecture Overview

```mermaid
flowchart TB
    subgraph External["External Client"]
        Client["HappyRobot / Client App"]
    end

    subgraph Gateway["IO.Proxy Gateway (Public)"]
        Proxy["X-Auth Middleware"]
        TF["TokenFactory Endpoint"]
        Router["Request Router"]
    end

    subgraph Internal["Internal Services (Private)"]
        Common["IO.Common"]
        Cass["IO.Cass"]
        Elsa["IO.Elsa"]
    end

    Client -->|"1. POST /api/tokens/generate<br/>X-Master-Token: {master}"| TF
    TF -->|"2. Returns scoped token"| Client
    Client -->|"3. GET /api/common/status<br/>Authorization: Bearer {scoped}"| Proxy
    Proxy -->|"4. Validate token & scopes"| Proxy
    Proxy -->|"5. Forward request"| Common
    Common -->|"6. Response"| Proxy
    Proxy -->|"7. Response"| Client
```

## Authentication Flows

### Flow 1: Token Generation (HappyRobot/Testing)

```mermaid
sequenceDiagram
    participant C as Client
    participant P as IO.Proxy
    participant TF as TokenFactory
    participant E as Environment Variables

    C->>P: POST /api/tokens/generate<br/>X-Master-Token: {master}
    P->>TF: Validate master token
    TF->>E: Read TOKEN_FACTORY__MASTER_TOKEN
    TF->>TF: Check type="master"<br/>Verify not expired
    alt Valid Master Token
        TF->>TF: Generate scoped token<br/>Sign with HMAC-SHA256
        TF->>P: Return ScopedTokenResult
        P->>C: 200 OK + token
    else Invalid Master Token
        TF->>P: InvalidMasterToken
        P->>C: 401 Unauthorized
    end
```

**Key Points**:
- Master token stored in `TOKEN_FACTORY__MASTER_TOKEN` env var
- Master token format: base64-encoded JSON with `type: "master"`
- Scoped token includes: token_id, scopes[], expires_at, signature
- Signature: HMAC-SHA256 of base64(payload) using `TOKEN_FACTORY__SECRET`

### Flow 2: API Request Authentication

```mermaid
sequenceDiagram
    participant C as Client
    participant M as X-Auth Middleware
    participant TF as TokenFactory
    participant S as Target Service

    C->>M: Request with Authorization: Bearer {token}
    
    alt Health/Swagger Path
        M->>S: Skip auth, forward request
    else Protected Path
        M->>M: Extract token from header
        
        alt Missing Token
            M->>C: 401 Unauthorized<br/>code: missing_token
        else Token Present
            M->>M: Detect token type
            
            alt Master Token Detected
                M->>C: 403 Forbidden<br/>Master tokens cannot access APIs
            else Scoped Token
                M->>TF: ValidateScopedToken(token)
                TF->>TF: Verify signature<br/>Check expiration
                
                alt Invalid Signature
                    TF->>M: null
                    M->>C: 401 Unauthorized<br/>code: invalid_signature
                else Expired Token
                    TF->>M: null
                    M->>C: 401 Unauthorized<br/>code: expired_token
                else Valid Token
                    TF->>M: ScopedTokenData
                    M->>M: Check required scopes
                    
                    alt Insufficient Scope
                        M->>C: 403 Forbidden<br/>code: insufficient_scope
                    else Valid Scope
                        M->>M: Add to HttpContext.Items:<br/>- XAuthToken<br/>- UserId<br/>- Scopes
                        M->>S: Forward request
                        S->>M: Response
                        M->>C: Response
                    end
                end
            end
        end
    end
```

### Flow 3: Python Pattern Compatibility

The Python authentication pattern (`token in scopes.scopes`) is replicated in C#:

```mermaid
flowchart LR
    subgraph Python["Python (auth.py)"]
        P1["SecurityScopes<br/>scopes.scopes"]
        P2["token in scopes"]
        P3["HTTPException 403<br/>if not authorized"]
    end

    subgraph CSharp["C# (XAuthTokenMiddleware)"]
        C1["GetRequiredScopesForRequest"]
        C2["requiredScopes.Any<br/>scope => tokenScopes<br/>.Contains"]
        C3["403 Forbidden<br/>insufficient_scope"]
    end

    P1 --> P2 --> P3
    C1 --> C2 --> C3
```

**Pattern Equivalence**:
| Python | C# |
|--------|-----|
| `SecurityScopes.scopes` | `requiredScopes` array from path |
| `token in scopes` | `requiredScopes.Any(s => tokenScopes.Contains(s))` |
| `HTTPException(403)` | `StatusCodes.Status403Forbidden` |

## Token Validation Rules

### Master Token Validation

```mermaid
flowchart TD
    A["Receive master token"] --> B["Base64 decode"]
    B --> C{"Valid JSON?"}
    C -->|No| D["Return: Invalid"]
    C -->|Yes| E{"type == 'master'?"}
    E -->|No| D
    E -->|Yes| F{"expires_at > now?"}
    F -->|No| D
    F -->|Yes| G["Optional: Verify signature"]
    G --> H["Return: Valid"]
```

### Scoped Token Validation

```mermaid
flowchart TD
    A["Receive scoped token"] --> B{"Contains '.'?"}
    B -->|No| C["Return: Malformed"]
    B -->|Yes| D["Split into parts"]
    D --> E["Base64 decode payload"]
    E --> F{"Valid JSON?"}
    F -->|No| C
    F -->|Yes| G{"type == 'scoped'?"}
    G -->|No| H["Return: Invalid type"]
    G -->|Yes| I["Compute HMAC-SHA256"]
    I --> J{"Signature matches?"}
    J -->|No| K["Return: Invalid signature"]
    J -->|Yes| L{"expires_at > now?"}
    L -->|No| M["Return: Expired"]
    L -->|Yes| N["Return: Valid + data"]
```

## Scope-Based Access Control

### Path-to-Scope Mapping

| Request Path | Required Scope |
|--------------|----------------|
| `/api/common/*` | `io-common-reader` or `io-common-writer` |
| `/api/cass/*` | `io-cass-reader` or `io-cass-writer` |
| `/api/elsa/*` | `io-elsa-reader` or `io-elsa-writer` |
| `/api/proxy/*` | `io-proxy-reader` or `io-proxy-writer` |

### Permission Model

```
io-{service}-reader: Read access (GET, HEAD)
io-{service}-writer: Write access (POST, PUT, DELETE, PATCH)
```

**Scope Hierarchy**:
- `*-writer` implies `*-reader` for the same service
- No cross-service access (cass token cannot access elsa)

## Security Considerations

### Zero-Trust Architecture

```mermaid
flowchart TB
    subgraph Principles["Zero-Trust Principles"]
        P1["1. Never trust, always verify"]
        P2["2. Assume breach"]
        P3["3. Verify explicitly"]
        P4["4. Use least privilege"]
    end

    subgraph Implementation["Our Implementation"]
        I1["Master token in env only<br/>Never in code/database"]
        I2["Short-lived tokens<br/>Max 24 hours"]
        I3["Cryptographic signatures<br/>HMAC-SHA256"]
        I4["Scope-based access<br/>Minimal permissions"]
    end

    P1 --> I1
    P2 --> I2
    P3 --> I3
    P4 --> I4
```

### Threat Mitigations

| Threat | Mitigation |
|--------|------------|
| Token theft | Short lifetime (1 hour default), scope-limited |
| Replay attacks | Token includes expiration timestamp |
| Tampering | HMAC-SHA256 signature verification |
| Master token exposure | Stored in env vars, never logged |
| Privilege escalation | Master tokens cannot access APIs |
| Token enumeration | UUID-based token IDs (unguessable) |

## Error Handling

### Error Response Format

All authentication errors return structured JSON:

```json
{
  "code": "error_code",
  "description": "Human readable message",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

### Error Code Mapping

| HTTP Status | Error Code | When Occurs |
|-------------|------------|-------------|
| 401 | `missing_token` | No Authorization or X-Auth-Token header |
| 401 | `invalid_token` | Token format not recognized |
| 401 | `expired_token` | Token past expiration time |
| 401 | `invalid_signature` | HMAC verification failed |
| 403 | `master_token_forbidden` | Master token used for API access |
| 403 | `insufficient_scope` | Token lacks required scope |

## Observability

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `auth_validation_total` | Counter | Total token validations |
| `auth_validation_duration` | Histogram | Validation latency (target <10ms) |
| `auth_validation_failed` | Counter | Failed validations by error code |
| `tokenfactory_generation_total` | Counter | Tokens generated by requester |
| `tokenfactory_generation_duration` | Histogram | Generation latency (target <50ms) |

### Logging

```csharp
// Validation success
_logger.LogInformation("Token validated successfully for {UserId} on {Path}", 
    userId, request.Path);

// Validation failure
_logger.LogWarning("Token validation failed: {ErrorCode} for {Path}", 
    errorCode, request.Path);

// Token generation (audit)
_logger.LogInformation("Scoped token generated: {TokenId} for {RequesterId} with scopes {Scopes}",
    tokenId, requesterId, scopes);
```

## Deployment Considerations

### Environment Variables

```yaml
# Required for TokenFactory
TOKEN_FACTORY__MASTER_TOKEN: "{base64-master-token}"  # Secret
TOKEN_FACTORY__SECRET: "{hmac-signing-secret}"       # Secret (min 32 chars)
TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS: "1"            # Config

# Optional for middleware
AUTH__API_TOKEN: "{static-dev-token}"               # Secret
AUTH__JWT_SECRET: "{jwt-validation-secret}"           # Secret
```

### Subdomain Routing

| Service | Subdomain | Public Access |
|---------|-----------|---------------|
| IO.Proxy | api.io.proxy | Yes (external entry point) |
| IO.Common | api.io.common | No (internal) |
| IO.Cass | api.io.cass | No (internal) |
| IO.Elsa | api.io.elsa | No (internal) |

## Testing Strategy

### Unit Tests

- `TokenFactoryTests.cs`: Token generation/validation logic
- `XAuthTokenMiddlewareTests.cs`: Middleware pipeline behavior

### Integration Tests

- End-to-end token generation → API access flow
- Expired token rejection
- Invalid signature detection
- Master token API access blocking

### Load Tests

- 1000 concurrent requests with token validation
- Target: <10ms p99 latency for validation
- Target: <50ms p99 latency for generation
