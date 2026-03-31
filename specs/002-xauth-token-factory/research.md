# Research: X-Auth Token Middleware and TokenFactory

**Branch**: `002-xauth-token-factory` | **Date**: 2026-03-31 | **Spec**: [spec.md](./spec.md)

## Unknowns Resolved

### 1. Python Authentication Pattern

**Decision**: The Python pattern uses `SecurityScopes` with a simple `token in scopes.scopes` check.

**Rationale**: The Python implementation in `python/src/helpers/auth.py` shows:
- Uses `fastapi.security.SecurityScopes` to define required scopes
- Validates token by checking if it exists in `scopes.scopes` list
- Returns 403 with structured error response for unauthorized access

**Implementation**: C# middleware will replicate this pattern with scope-based validation.

---

### 2. TokenFactory Cryptographic Signing

**Decision**: Use HMAC-SHA256 for scoped token signing with base64-encoded JSON payload.

**Rationale**:
- TokenFactory already implements HMAC-SHA256 signing in `TokenFactory.cs`
- Format: `{base64(json)}.{signature}` enables easy parsing and validation
- Master token uses base64-encoded JSON with `type: "master"` field
- No external dependencies needed beyond `System.Security.Cryptography`

**Alternatives Considered**:
- JWT with Microsoft.IdentityModel.Tokens - adds complexity, not needed for internal tokens
- EdDSA - excellent security but unnecessary for internal scoped tokens with short lifetime

---

### 3. MSAL Integration Strategy

**Decision**: Implement MSAL validation as future-proofing option, scoped tokens as primary.

**Rationale**:
- XAuthTokenMiddleware already has MSAL validation path for JWT tokens
- TokenFactory provides immediate zero-trust capability
- MSAL integration prepared for OAuth/OIDC upgrade path
- MSAL requires `ClientId`, `Authority`, `TenantId`, and certificate/secret

---

### 4. Constants Pattern

**Decision**: Extend existing `AuthenticationConstants.cs` with environment variable constants.

**Rationale**:
- `AuthenticationConstants` already defines headers, error codes, and TokenFactory env vars
- Pattern exists in `src/Common/Core/Constants/AuthenticationConstants.cs`
- Need to add: MongoDB collections, Kafka topics, service endpoints constants

---

### 5. Subdomain Routing Pattern

**Decision**: Configure all services with `product: io`, `app: {service}`, `type: api` pattern.

**Rationale**:
- io-proxy-api.yaml: `public.enabled: true` with subdomain routing
- io-common-api.yaml: No `public` section (internal only)
- Pattern follows DX documentation: `api.io.{service}` format

**Services to Configure**:
- api.io.proxy (public)
- api.io.common (internal)
- api.io.cass (internal)
- api.io.elsa (internal)
- api.io.larry (internal)
- api.io.lea (internal)

---

### 6. Environment Variable Naming

**Decision**: Follow USXpress double-underscore convention.

**Rationale**:
- Already implemented: `AUTH__API_TOKEN`, `TOKEN_FACTORY__SECRET`
- Configuration binding uses `:` separator in code, `__` in environment
- Examples from existing code:
  - `AUTH:API_TOKEN` in code → `AUTH__API_TOKEN` in env
  - `TOKEN_FACTORY:SECRET` in code → `TOKEN_FACTORY__SECRET` in env

---

## Technology Choices

| Component | Choice | Rationale |
|-----------|--------|-----------|
| Token Signing | HMAC-SHA256 | Fast, secure for internal tokens, no cert management |
| Token Format | Base64.JSON.Signature | Simple parsing, human-readable payload |
| Middleware | ASP.NET Core Middleware | Standard pattern, integrates with pipeline |
| Configuration | IOptions pattern | Type-safe, validates at startup |
| Logging | USXpress.Monitoring | Constitution requirement, integrated with Grafana |

---

## Integration Points

### XAuthTokenMiddleware → TokenFactory
- Middleware validates scoped tokens via `TokenFactory.ValidateScopedToken()`
- Middleware rejects master tokens for API access (security requirement)

### TokenFactory → Environment Variables
- Reads `TOKEN_FACTORY__MASTER_TOKEN` from environment
- Reads `TOKEN_FACTORY__SECRET` for signing
- Uses `TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS` (optional, defaults to 1)

### Middleware → Downstream Services
- Adds validated token info to `HttpContext.Items`
- Downstream services can access `UserId`, `Scopes` from context

---

## Performance Baselines

Based on code analysis:
- Token validation (scoped): <1ms (HMAC-SHA256 is fast)
- Token generation: <10ms (JSON serialization + HMAC)
- Middleware overhead: <5ms (path check + token extraction)

These meet success criteria SC-001 (<10ms overhead) and SC-002 (<50ms generation).
