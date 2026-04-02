# Data Model: X-Auth Token Middleware and TokenFactory

**Branch**: `002-xauth-token-factory` | **Date**: 2026-03-31 | **Spec**: [spec.md](./spec.md)

## Entity: XAuthToken

Represents authentication token with scope validation for API access control.

### Fields

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| token_value | string | The raw token string (Bearer token or X-Auth-Token) | Required, non-empty |
| source | enum | Token source: `AuthorizationHeader`, `XAuthTokenHeader` | One of defined sources |

### State Transitions

N/A - Stateless validation pattern

---

## Entity: TokenFactory

Represents zero-trust token generation system with master token and scoped token lifecycle.

### Fields

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| factory_secret | string | HMAC-SHA256 signing secret | Required, minimum 32 characters |
| master_token | string | Base64-encoded master token from env | Required for operation |
| default_lifetime | TimeSpan | Default scoped token lifetime | Default: 1 hour, max: 24 hours |

### State Transitions

1. **Initialization**: Factory loads master token from `TOKEN_FACTORY__MASTER_TOKEN` environment variable
2. **Token Generation**: Master token validated → Scoped token generated with signature
3. **Token Validation**: Scoped token parsed → Signature verified → Expiration checked

---

## Entity: MasterTokenData

Represents the master token structure with type="master".

### Fields

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| type | string | Token type, always "master" | Must equal "master" |
| expires_at | long | Unix timestamp for expiration | Must be in future |
| generated_at | long | Unix timestamp when created | Required |
| purpose | string | Token purpose description | Optional |
| signature | string | Optional HMAC signature | Optional verification |

### JSON Schema

```json
{
  "type": "object",
  "required": ["type", "expires_at", "generated_at"],
  "properties": {
    "type": { "type": "string", "enum": ["master"] },
    "expires_at": { "type": "integer", "minimum": 0 },
    "generated_at": { "type": "integer", "minimum": 0 },
    "purpose": { "type": "string" },
    "signature": { "type": "string" }
  }
}
```

---

## Entity: ScopedTokenData

Represents generated scoped tokens with scopes, expiration, and cryptographic signature.

### Fields

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| token_id | string | UUID for token identification | Required, UUID format |
| scopes | string[] | Array of permission scopes | Required, at least one scope |
| expires_at | long | Unix timestamp for expiration | Must be in future |
| generated_at | long | Unix timestamp when created | Required |
| generated_by | string | Token generator identifier | Default: "token-factory" |
| requester_id | string | ID of requesting entity | Required |
| type | string | Token type, always "scoped" | Must equal "scoped" |

### JSON Schema

```json
{
  "type": "object",
  "required": ["token_id", "scopes", "expires_at", "generated_at", "requester_id", "type"],
  "properties": {
    "token_id": { "type": "string", "format": "uuid" },
    "scopes": { 
      "type": "array", 
      "items": { "type": "string" },
      "minItems": 1
    },
    "expires_at": { "type": "integer", "minimum": 0 },
    "generated_at": { "type": "integer", "minimum": 0 },
    "generated_by": { "type": "string" },
    "requester_id": { "type": "string" },
    "type": { "type": "string", "enum": ["scoped"] }
  }
}
```

### Token Format

The scoped token is a compound string:

```
{base64(scoped_token_json)}.{hmac_sha256_signature}
```

Example:
```
eyJ0b2tlbl9pZCI6IjEyMy4uLiIsInNjb3BlcyI6WyJpby1jb21tb24tcmVhZGVyIl0s... .aBcD123...
```

---

## Entity: ScopedTokenResult

Result object returned from token generation.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| token | string | The complete scoped token string |
| token_id | string | UUID for this token |
| scopes | string[] | Granted scopes |
| expires_at | DateTime | When token expires |
| generated_at | DateTime | When token was created |

---

## Entity: TokenValidationResult

Result from token validation service.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| is_valid | boolean | Whether token is valid and authorized |
| user_id | string | Extracted user/requester ID |
| scopes | string[] | Extracted scopes from token |
| error_code | string | Error code if invalid (null if valid) |
| error_message | string | Human-readable error description |
| expires_at | DateTime | Token expiration time |

### Validation Rules

1. **Token Format**: Must contain exactly one `.` separator
2. **Base64 Decoding**: Payload must be valid base64
3. **JSON Parsing**: Payload must be valid JSON matching ScopedTokenData schema
4. **Signature Verification**: HMAC-SHA256 must match computed value
5. **Expiration**: `expires_at` must be greater than current Unix timestamp
6. **Scope Matching**: At least one token scope must match required endpoint scopes

---

## Entity: AuthenticationScopes (Constants)

Permission scopes for different IO services.

### Values

| Constant | Value | Description |
|----------|-------|-------------|
| ProxyRead | io-proxy-reader | Read access to Proxy gateway |
| ProxyWrite | io-proxy-writer | Write access to Proxy gateway |
| CommonRead | io-common-reader | Read access to Common service |
| CommonWrite | io-common-writer | Write access to Common service |
| CassRead | io-cass-reader | Read access to Cass service |
| CassWrite | io-cass-writer | Write access to Cass service |
| ElsaRead | io-elsa-reader | Read access to Elsa service |
| ElsaWrite | io-elsa-writer | Write access to Elsa service |

---

## Entity: TokenFactoryOptions

Configuration options for TokenFactory.

### Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| factory_secret | string | HMAC-SHA256 signing secret | "default-factory-secret-change-in-production" |
| master_token | string | Master token from environment | null (loads from env) |
| default_token_lifetime | TimeSpan | Default scoped token lifetime | 1 hour |

---

## Entity: XAuthTokenOptions

Configuration options for X-Auth middleware.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| api_token | string | Static API token for development |
| validation_endpoint | string | External validation service URL |
| validation_token | string | Token for validation service auth |
| jwt_secret | string | Secret for JWT validation |
| client_id | string | MSAL client ID |
| client_secret | string | MSAL client secret |
| authority | string | MSAL authority URL |
| tenant_id | string | Azure AD tenant ID |
| certificate | X509Certificate2 | Certificate for MSAL auth |

---

## Relationships

```
┌─────────────────────┐       uses       ┌──────────────────────┐
│  XAuthTokenMiddleware│◄─────────────────│     TokenFactory     │
├─────────────────────┤                  ├──────────────────────┤
│ - Validates tokens  │                  │ - Generates tokens   │
│ - Checks scopes     │                  │ - Signs with HMAC    │
│ - Extracts from     │                  │ - Validates master   │
│   headers           │                  │   tokens             │
└─────────┬───────────┘                  └──────────┬─────────┘
          │                                           │
          │ validates                                 │ produces
          │                                           │
          ▼                                           ▼
┌─────────────────────┐                  ┌──────────────────────┐
│  ScopedTokenData    │◄─────────────────│  MasterTokenData   │
├─────────────────────┤   generates      ├──────────────────────┤
│ - token_id          │                  │ - type: "master"     │
│ - scopes[]          │                  │ - expires_at         │
│ - expires_at        │                  │ - generated_at       │
│ - signature         │                  │ - signature          │
└─────────────────────┘                  └──────────────────────┘
```

---

## Validation Rules Summary

### Master Token Validation

1. Must be valid base64
2. Decoded JSON must have `type: "master"`
3. `expires_at` must be in the future
4. Optional signature verification if `MasterTokenSignature` configured

### Scoped Token Validation

1. Must contain exactly one `.` character
2. Payload part must be valid base64
3. Payload must deserialize to `ScopedTokenData`
4. `type` must equal `"scoped"`
5. HMAC-SHA256 signature must match computed value
6. `expires_at` must be greater than current Unix timestamp
7. At least one scope in `scopes` must match required endpoint scopes

### Master Token Cannot Access APIs

1. Middleware checks if token starts with known master token prefix
2. If master token format detected, request is rejected with 403
3. Master tokens only valid for TokenFactory generation endpoint
