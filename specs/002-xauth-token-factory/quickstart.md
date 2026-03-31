# Quickstart: X-Auth Token Middleware and TokenFactory

**Branch**: `002-xauth-token-factory` | **Date**: 2026-03-31 | **Spec**: [spec.md](./spec.md)

## Prerequisites

- .NET 10 SDK installed
- Docker (for local development with containerized services)
- Access to IO Platform repository
- kubectl access to dev cluster (for deployment verification)

## Local Development Setup

### 1. Clone and Checkout

```powershell
git clone <repository-url>
cd python-demo-poc
git checkout 002-xauth-token-factory
```

### 2. Generate Master Token (for testing)

The TokenFactory requires a master token from environment variables. Generate one for local testing:

```powershell
# Run the master token generation (requires .NET 10)
cd src/Common/Core/Authentication
dotnet run --project TokenFactory.cs -- generate-master --valid-for 30d

# Or use the extension method programmatically:
var masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(30));
```

### 3. Configure Environment Variables

Create a `.env` file in the repository root:

```env
# TokenFactory Configuration
TOKEN_FACTORY__MASTER_TOKEN=<your-generated-master-token>
TOKEN_FACTORY__SECRET=dev-factory-secret-change-in-production
TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS=1

# Authentication (for middleware)
AUTH__API_TOKEN=dev-api-token-for-testing
AUTH__JWT_SECRET=dev-jwt-secret

# Monitoring
APPLICATION_PROJECT=io-platform
APPLICATION_GROUP=gateway
APPLICATION_ENVIRONMENT=development
```

### 4. Build the Project

```powershell
cd src/Apps/RestAPI/IO.Proxy
dotnet build
dotnet run
```

The API will start on `https://localhost:8080` (or configured port).

### 5. Verify Health Endpoints

```powershell
# Health check (no auth required)
curl http://localhost:8080/health

# Readiness check (no auth required)
curl http://localhost:8080/ready
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

## Token Generation (HappyRobot/Testing)

### Generate Scoped Tokens

Call the TokenFactory endpoint with a master token:

```powershell
# Generate a scoped token for Common service
$headers = @{
    "X-Master-Token" = "<your-master-token>"
    "Content-Type" = "application/json"
}

$body = @{
    scopes = @("io-common-reader", "io-common-writer")
    requesterId = "happy-robot-common"
    validForMinutes = 60
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8080/api/tokens/generate" -Method POST -Headers $headers -Body $body

# The response contains the scoped token
$scopedToken = $response.token
```

### Generate All Service Tokens

```powershell
# Generate tokens for all IO services at once
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/tokens/generate-all" -Method POST -Headers $headers

# Access individual service tokens
$commonToken = $response.common.token
$cassToken = $response.cass.token
$elsaToken = $response.elsa.token
```

## Making Authenticated Requests

### Using Bearer Token

```powershell
$headers = @{
    "Authorization" = "Bearer $scopedToken"
}

# Call Common service through Proxy
Invoke-RestMethod -Uri "http://localhost:8080/api/common/status" -Headers $headers
```

### Using X-Auth-Token Header

```powershell
$headers = @{
    "X-Auth-Token" = $scopedToken
}

# Call Cass service through Proxy
Invoke-RestMethod -Uri "http://localhost:8080/api/cass/carriers" -Headers $headers
```

## Testing Authentication Flows

### Test 1: Valid Token with Correct Scope

```powershell
# Should succeed - token has common-read scope
$headers = @{ "Authorization" = "Bearer $commonToken" }
Invoke-RestMethod -Uri "http://localhost:8080/api/common/emails" -Headers $headers
```

### Test 2: Valid Token with Insufficient Scope

```powershell
# Should fail with 403 - common token cannot access cass endpoints
$headers = @{ "Authorization" = "Bearer $commonToken" }
Invoke-RestMethod -Uri "http://localhost:8080/api/cass/carriers" -Headers $headers
# Expected: 403 Forbidden with error code "insufficient_scope"
```

### Test 3: Missing Token

```powershell
# Should fail with 401 - no authentication provided
Invoke-RestMethod -Uri "http://localhost:8080/api/common/status"
# Expected: 401 Unauthorized with error code "missing_token"
```

### Test 4: Master Token Cannot Access APIs

```powershell
# Should fail with 403 - master tokens cannot access API endpoints
$headers = @{ "Authorization" = "Bearer <master-token>" }
Invoke-RestMethod -Uri "http://localhost:8080/api/common/status" -Headers $headers
# Expected: 403 Forbidden - master tokens only valid for token generation
```

### Test 5: Expired Token

```powershell
# Generate a token with 1 second lifetime
$body = @{
    scopes = @("io-common-reader")
    validForMinutes = 0.016  # 1 second
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8080/api/tokens/generate" -Method POST -Headers $headers -Body $body
$shortLivedToken = $response.token

# Wait 2 seconds
Start-Sleep -Seconds 2

# Should fail with 401 - token expired
$headers = @{ "Authorization" = "Bearer $shortLivedToken" }
Invoke-RestMethod -Uri "http://localhost:8080/api/common/status" -Headers $headers
# Expected: 401 Unauthorized with error code "expired_token"
```

## Debugging

### Enable Detailed Logging

Set environment variables for verbose logging:

```powershell
$env:Serilog__MinimumLevel__Default = "Debug"
dotnet run
```

### Check Middleware Pipeline

Verify middleware is registered correctly:

```csharp
// In Program.cs - should see these lines
app.UseXAuthTokenAuthentication();  // Must be before UseAuthorization
app.UseAuthorization();
```

### Validate Token Manually

```powershell
# Decode a scoped token to inspect payload
$token = "eyJ0b2tlbl9pZ... .signature..."
$parts = $token.Split('.')
$payload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[0]))
$payload | ConvertFrom-Json
```

## Common Issues

### Issue: TokenFactory returns "Invalid master token"

**Cause**: Master token expired or environment variable not set

**Fix**:
```powershell
# Check environment variable
$env:TOKEN_FACTORY__MASTER_TOKEN

# Regenerate master token
$masterToken = TokenFactory.GenerateMasterToken(TimeSpan.FromDays(30))
```

### Issue: 401 on all requests including valid tokens

**Cause**: Middleware not registered in pipeline

**Fix**: Ensure `app.UseXAuthTokenAuthentication()` is called in `Program.cs`

### Issue: Scope validation fails despite correct scopes

**Cause**: Path-based scope mapping doesn't match endpoint

**Fix**: Check `GetRequiredScopesForRequest()` in middleware matches your API routes

## Deployment Verification

### Verify Subdomain Routing

```powershell
# Dev environment should resolve:
# api.io.proxy.dev.usxpress.io - Proxy (public)
# api.io.common.dev.usxpress.io - Common (internal)

# Verify DNS
dig api.io.proxy.dev.usxpress.io
```

### Verify TokenFactory Endpoint

```powershell
# Call production TokenFactory endpoint
$headers = @{ "X-Master-Token" = $env:TOKEN_FACTORY__MASTER_TOKEN }
Invoke-RestMethod -Uri "https://api.io.proxy.dev.usxpress.io/api/tokens/generate" -Method POST -Headers $headers -Body $body
```

## Next Steps

1. **Integration Tests**: Run `dotnet test` in `tests/integration/` directory
2. **Load Testing**: Use k6 or similar to validate <10ms validation overhead
3. **Security Audit**: Verify master tokens cannot access API endpoints
4. **Documentation**: Update README.md with authentication examples
