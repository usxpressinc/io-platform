# TokenFactory API Contract

**Version**: 1.0.0 | **Base URL**: `https://api.io.proxy.{env}.usxpress.io/api/tokens`

## Overview

The TokenFactory API provides endpoints for generating cryptographically signed scoped tokens using a master token. This enables zero-trust authentication where master tokens (stored in environment variables) cannot access API endpoints directly but can generate time-limited scoped tokens.

## Authentication

All TokenFactory endpoints require the master token in the `X-Master-Token` header.

```http
X-Master-Token: {base64-encoded-master-token}
```

The master token is a base64-encoded JSON object with the following structure:

```json
{
  "type": "master",
  "expires_at": 1711929600,
  "generated_at": 1709337600,
  "purpose": "token-generation"
}
```

## Endpoints

### POST /generate

Generate a single scoped token with specific scopes.

#### Request

```http
POST /api/tokens/generate
Content-Type: application/json
X-Master-Token: {master-token}

{
  "scopes": ["io-common-reader", "io-common-writer"],
  "requesterId": "happy-robot-common",
  "validForMinutes": 60
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| scopes | string[] | Yes | Array of permission scopes to grant |
| requesterId | string | No | Identifier for audit/logging (default: "unknown") |
| validForMinutes | number | No | Token lifetime in minutes (default: 60, max: 1440) |

#### Response

**200 OK** - Token generated successfully

```json
{
  "token": "eyJ0b2tlbl9pZCI6IjEyMy4uLiIsInNjb3BlcyI6WyJpby1jb21tb24tcmVhZGVyIl0s... .signature...",
  "tokenId": "550e8400-e29b-41d4-a716-446655440000",
  "scopes": ["io-common-reader", "io-common-writer"],
  "expiresAt": "2026-03-31T21:00:00Z",
  "generatedAt": "2026-03-31T20:00:00Z"
}
```

**400 Bad Request** - Invalid request parameters

```json
{
  "code": "invalid_request",
  "description": "Scopes array must contain at least one scope",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

**401 Unauthorized** - Invalid or expired master token

```json
{
  "code": "invalid_master_token",
  "description": "Master token is invalid or has expired",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

**403 Forbidden** - Insufficient permissions

```json
{
  "code": "forbidden",
  "description": "Master token format not recognized",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

---

### POST /generate-all

Generate scoped tokens for all IO services simultaneously.

#### Request

```http
POST /api/tokens/generate-all
Content-Type: application/json
X-Master-Token: {master-token}

{
  "requesterId": "happy-robot",
  "validForMinutes": 60
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| requesterId | string | No | Identifier for audit/logging |
| validForMinutes | number | No | Token lifetime in minutes |

#### Response

**200 OK** - All tokens generated successfully

```json
{
  "common": {
    "token": "eyJ0b2tlbl9pZCI6... .signature...",
    "tokenId": "550e8400-e29b-41d4-a716-446655440000",
    "scopes": ["io-common-reader", "io-common-writer"],
    "expiresAt": "2026-03-31T21:00:00Z",
    "generatedAt": "2026-03-31T20:00:00Z"
  },
  "cass": {
    "token": "eyJ0b2tlbl9pZCI6... .signature...",
    "tokenId": "660e8400-e29b-41d4-a716-446655440001",
    "scopes": ["io-cass-reader", "io-cass-writer"],
    "expiresAt": "2026-03-31T21:00:00Z",
    "generatedAt": "2026-03-31T20:00:00Z"
  },
  "elsa": {
    "token": "eyJ0b2tlbl9pZCI6... .signature...",
    "tokenId": "770e8400-e29b-41d4-a716-446655440002",
    "scopes": ["io-elsa-reader", "io-elsa-writer"],
    "expiresAt": "2026-03-31T21:00:00Z",
    "generatedAt": "2026-03-31T20:00:00Z"
  }
}
```

**401 Unauthorized** - Invalid or expired master token

```json
{
  "code": "invalid_master_token",
  "description": "Master token is invalid or has expired",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

---

### POST /validate

Validate a scoped token and return its metadata.

#### Request

```http
POST /api/tokens/validate
Content-Type: application/json

{
  "token": "eyJ0b2tlbl9pZCI6... .signature..."
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| token | string | Yes | The scoped token to validate |

#### Response

**200 OK** - Token is valid

```json
{
  "isValid": true,
  "tokenId": "550e8400-e29b-41d4-a716-446655440000",
  "scopes": ["io-common-reader", "io-common-writer"],
  "requesterId": "happy-robot-common",
  "expiresAt": "2026-03-31T21:00:00Z",
  "generatedAt": "2026-03-31T20:00:00Z"
}
```

**200 OK** - Token is invalid (still HTTP 200, but isValid=false)

```json
{
  "isValid": false,
  "errorCode": "expired_token",
  "errorMessage": "Token has expired"
}
```

**400 Bad Request** - Malformed token

```json
{
  "code": "malformed_token",
  "description": "Token format is invalid",
  "timestamp": "2026-03-31T20:00:00Z"
}
```

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `invalid_master_token` | 401 | Master token is invalid or expired |
| `invalid_request` | 400 | Request body is malformed or missing required fields |
| `malformed_token` | 400 | Token format is invalid (missing signature, bad base64) |
| `expired_token` | 401 | Token has passed its expiration time |
| `invalid_signature` | 401 | Token signature verification failed |
| `forbidden` | 403 | Master token cannot be used for this operation |
| `rate_limited` | 429 | Too many token generation requests |
| `internal_error` | 500 | Unexpected server error |

## Rate Limiting

- **Generate endpoints**: 100 requests per minute per master token
- **Validate endpoint**: 1000 requests per minute per IP

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1711929600
```

## Token Format Specification

### Scoped Token Structure

```
{base64-encoded-json-payload}.{base64-encoded-hmac-signature}
```

Example:
```
eyJ0b2tlbl9pZCI6IjEyMy4uLiIsInNjb3BlcyI6WyJpby1jb21tb24tcmVhZGVyIl0s... .aBcD123...
```

### Payload Schema

```json
{
  "token_id": "550e8400-e29b-41d4-a716-446655440000",
  "scopes": ["io-common-reader", "io-common-writer"],
  "expires_at": 1711929600,
  "generated_at": 1711926000,
  "generated_by": "token-factory",
  "requester_id": "happy-robot-common",
  "type": "scoped"
}
```

### Signature Algorithm

- **Algorithm**: HMAC-SHA256
- **Key**: `TOKEN_FACTORY__SECRET` environment variable
- **Input**: Base64-encoded JSON payload
- **Output**: Base64-encoded 256-bit signature

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `TOKEN_FACTORY__MASTER_TOKEN` | Yes | Base64-encoded master token |
| `TOKEN_FACTORY__SECRET` | Yes | HMAC-SHA256 signing secret (min 32 chars) |
| `TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS` | No | Default token lifetime (default: 1) |

## Authentication Scopes Reference

| Scope | Service | Permission |
|-------|---------|------------|
| `io-proxy-reader` | Proxy | Read access |
| `io-proxy-writer` | Proxy | Write access |
| `io-common-reader` | Common | Read access |
| `io-common-writer` | Common | Write access |
| `io-cass-reader` | Cass | Read access |
| `io-cass-writer` | Cass | Write access |
| `io-elsa-reader` | Elsa | Read access |
| `io-elsa-writer` | Elsa | Write access |

## OpenAPI Specification

```yaml
openapi: 3.0.3
info:
  title: TokenFactory API
  version: 1.0.0
  description: Zero-trust token generation service

paths:
  /api/tokens/generate:
    post:
      summary: Generate scoped token
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [scopes]
              properties:
                scopes:
                  type: array
                  items:
                    type: string
                requesterId:
                  type: string
                validForMinutes:
                  type: number
      responses:
        '200':
          description: Token generated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ScopedTokenResult'
        '401':
          description: Invalid master token
        '400':
          description: Invalid request

  /api/tokens/generate-all:
    post:
      summary: Generate all service tokens
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                requesterId:
                  type: string
                validForMinutes:
                  type: number
      responses:
        '200':
          description: All tokens generated
          content:
            application/json:
              schema:
                type: object
                additionalProperties:
                  $ref: '#/components/schemas/ScopedTokenResult'

  /api/tokens/validate:
    post:
      summary: Validate scoped token
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [token]
              properties:
                token:
                  type: string
      responses:
        '200':
          description: Validation result
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ValidationResult'

components:
  schemas:
    ScopedTokenResult:
      type: object
      properties:
        token:
          type: string
        tokenId:
          type: string
          format: uuid
        scopes:
          type: array
          items:
            type: string
        expiresAt:
          type: string
          format: date-time
        generatedAt:
          type: string
          format: date-time

    ValidationResult:
      type: object
      properties:
        isValid:
          type: boolean
        tokenId:
          type: string
        scopes:
          type: array
          items:
            type: string
        requesterId:
          type: string
        expiresAt:
          type: string
          format: date-time
        generatedAt:
          type: string
          format: date-time
        errorCode:
          type: string
        errorMessage:
          type: string
```
