# Feature Specification: X-Auth Token Middleware and TokenFactory

**Feature Branch**: `002-xauth-token-factory`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "Update spec with X-Auth token middleware and TokenFactory implementation for zero-trust authentication"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Per Constitution Principle II: Each story must be independently deployable as a microservice slice.
  Per Constitution Principle V: Independent testing includes unit, integration, and e2e validation.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently  
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - X-Auth Token Middleware Implementation (Priority: P1)

As a system architect, I want to implement X-Auth token middleware for the IO.Proxy API gateway that validates tokens using scope-based authentication matching the existing Python pattern, so that external clients can authenticate requests while maintaining compatibility with the current authentication flow.

**Why this priority**: Authentication is foundational for all API access. The X-Auth middleware must replicate the Python authentication pattern (checking if token is in allowed scopes) while adding production-ready features like JWT validation and MSAL integration for future OAuth support.

**Independent Test**: Can be fully tested by configuring the middleware with test tokens and verifying that requests with valid tokens are accepted while invalid tokens are rejected with appropriate HTTP status codes.

**Acceptance Scenarios**:

1. **Given** the X-Auth middleware is configured, **When** a request with valid Bearer token in allowed scopes is received, **Then** the request is forwarded to downstream services with authentication context
2. **Given** the X-Auth middleware is configured, **When** a request with missing Authorization header is received, **Then** the middleware returns 401 Unauthorized with structured error response
3. **Given** the X-Auth middleware is configured, **When** a request with token not in allowed scopes is received, **Then** the middleware returns 403 Forbidden with scope validation error
4. **Given** health endpoints are configured, **When** a request to /health or /ready is received, **Then** authentication is bypassed and health status is returned
5. **Given** Swagger endpoints are configured, **When** a request to /swagger is received, **Then** authentication is bypassed for documentation access

---

### User Story 2 - TokenFactory for Zero-Trust Runtime Token Generation (Priority: P1)

As a DevOps engineer, I want to implement a TokenFactory system that uses an environment-variable-based master token to generate scoped tokens at runtime, so that HappyRobot and other testing tools can obtain time-limited tokens without database storage or permanent API keys.

**Why this priority**: Zero-trust architecture requires that no permanent credentials exist in the system. The TokenFactory enables dynamic token generation where the master token (stored securely in environment variables) can only generate scoped tokens, not access APIs directly.

**Independent Test**: Can be fully tested by configuring the TokenFactory with a master token from environment variables and verifying that (1) master token cannot access API endpoints, (2) scoped tokens can be generated using the master token, (3) scoped tokens expire correctly, and (4) token validation works with cryptographic signatures.

**Acceptance Scenarios**:

1. **Given** TOKEN_FACTORY__MASTER_TOKEN is configured in environment, **When** the TokenFactory service starts, **Then** it successfully loads the master token and factory secret
2. **Given** a valid master token, **When** a scoped token generation request is made with specific scopes, **Then** a signed token with those scopes and expiration time is returned
3. **Given** a master token, **When** an attempt is made to use it as an API authentication token, **Then** the request is rejected (master token cannot access APIs)
4. **Given** a valid scoped token, **When** the token is presented to a protected endpoint, **Then** the request is allowed if scopes match the endpoint requirements
5. **Given** a scoped token with limited lifetime, **When** the token expires, **Then** subsequent validation attempts return null/invalid
6. **Given** an invalid or tampered token, **When** validation is attempted, **Then** cryptographic signature verification fails and token is rejected

---

### User Story 3 - Constants Pattern Implementation (Priority: P2)

As a developer, I want to implement centralized constants classes for MongoDB collections, Kafka topics, environment variables, service endpoints, and authentication scopes, so that configuration values are consistent across all services and changes can be made in a single location.

**Why this priority**: Following the constitution principle of maintainability, constants ensure that service names, topic names, and configuration keys are never hardcoded as magic strings, reducing bugs and improving discoverability.

**Independent Test**: Can be fully tested by referencing the constants classes in code and verifying that compilation succeeds and all expected constants are defined with correct values.

**Acceptance Scenarios**:

1. **Given** the Constants classes are defined, **When** MongoDB collection names are referenced, **Then** they use constants like `MongoDbCollections.VendorCollection` instead of string literals
2. **Given** the Constants classes are defined, **When** Kafka topic names are referenced, **Then** they use constants like `KafkaTopics.VendorLookupEvent`
3. **Given** the Constants classes are defined, **When** environment variables are referenced, **Then** they use constants like `EnvironmentVariables.MongoConnectionString`
4. **Given** the Constants classes are defined, **When** authentication scopes are checked, **Then** they use constants like `AuthenticationScopes.CommonRead`

---

### User Story 4 - Subdomain Routing Configuration (Priority: P2)

As a platform engineer, I want to configure subdomain routing for all IO services using the pattern `api.io.{service}` with public access only on the proxy service, so that external clients access the platform through a unified entry point while internal services communicate privately.

**Why this priority**: Following DX documentation patterns, proper subdomain routing enables clean URL structures (api.io.proxy, api.io.common, etc.) and ensures only the gateway is externally accessible, with internal services protected behind the proxy.

**Independent Test**: Can be fully tested by deploying the configuration and verifying that DNS resolution and routing work correctly for each subdomain, with the proxy being the only public endpoint.

**Acceptance Scenarios**:

1. **Given** deployment YAMLs are configured, **When** services are deployed, **Then** each service is accessible via its subdomain (api.io.proxy, api.io.common, api.io.cass, etc.)
2. **Given** the proxy deployment configuration, **When** public access is checked, **Then** only io.proxy has `public.enabled: true`
3. **Given** internal service deployment configurations, **When** public access is checked, **Then** internal services (io.common, io.cass, etc.) do not have public access enabled
4. **Given** the proxy is configured with assigned_to_apps, **When** internal communication occurs, **Then** the proxy uses API-to-API flow to communicate with downstream services

---

### Edge Cases

- What happens when the master token environment variable is not set? (TokenFactory should fail to start or operate in development mode with warning)
- How does the system handle a scoped token that is presented after expiration? (Validation should return null and middleware should reject with 401)
- What happens when the factory secret is changed? (Existing scoped tokens become invalid due to signature mismatch)
- How does the middleware handle concurrent requests with the same token? (Each request is validated independently, thread-safe)
- What happens when a request includes both Authorization header and X-Auth-Token header? (Authorization header takes precedence per implementation)
- How does the system handle a master token that is used to attempt API access? (Request should be rejected with 403, as master tokens cannot access APIs)
- What happens when an endpoint has no scope requirements defined? (Request should be allowed, no authentication needed for that endpoint)
- How does the system handle a token with valid format but invalid signature? (Cryptographic verification should fail and reject the token)
- What happens when the TokenFactory service is unavailable during token generation? (Request should fail gracefully with 503 service unavailable)
- How does the middleware behave during high load? (Token validation should remain under 10ms overhead even with 1000+ concurrent requests)

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: X-Auth token middleware MUST validate tokens against allowed scopes following Python authentication pattern (token in scopes check)
- **FR-002**: X-Auth token middleware MUST support both `Authorization: Bearer` header and `X-Auth-Token` header for token extraction
- **FR-003**: X-Auth token middleware MUST bypass authentication for health endpoints (/health, /ready) and documentation endpoints (/swagger)
- **FR-004**: X-Auth token middleware MUST return structured JSON error responses with appropriate HTTP status codes (401, 403)
- **FR-005**: X-Auth token middleware MUST support JWT token validation via MSAL for future OAuth integration
- **FR-006**: TokenFactory MUST read master token from TOKEN_FACTORY__MASTER_TOKEN environment variable
- **FR-007**: TokenFactory MUST generate cryptographically signed scoped tokens with configurable lifetime
- **FR-008**: TokenFactory MUST validate master token format (type="master") before allowing scoped token generation
- **FR-009**: Master token MUST be unable to access API endpoints (security validation required)
- **FR-010**: Scoped tokens MUST include scopes, expiration time, token ID, and requester ID in signed payload
- **FR-011**: TokenFactory MUST provide token validation endpoint for testing scoped tokens
- **FR-012**: Constants classes MUST be defined for MongoDB collections, Kafka topics, environment variables, service endpoints, and authentication scopes
- **FR-013**: All configuration references MUST use constants instead of magic strings
- **FR-014**: Deployment YAMLs MUST configure subdomain routing with `product: io`, `app: {service}`, `type: api` pattern
- **FR-015**: Only IO.Proxy deployment MUST have `public.enabled: true` for external access
- **FR-016**: Environment variables MUST be nested under deployment type (api) per DX documentation requirements

### Key Entities *(include if feature involves data)*

- **XAuthToken**: Represents authentication token with scope validation for API access control, supporting Bearer and X-Auth-Token headers
- **TokenFactory**: Represents zero-trust token generation system with master token and scoped token lifecycle, using environment variables and cryptographic signing
- **AuthenticationScopes**: Represents service-specific permission scopes (common-read, common-write, cass-read, cass-write, etc.)
- **MasterTokenData**: Represents the master token structure with type="master", expiration, and generation metadata
- **ScopedTokenData**: Represents generated scoped tokens with scopes, expiration, token ID, requester ID, and cryptographic signature
- **TokenValidationResult**: Represents the result of token validation with validity status, extracted scopes, user ID, and error information

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  Per Constitution Principle IV: All criteria must be observable via monitoring/telemetry.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: X-Auth token middleware validates 100% of requests with <10ms overhead per request
- **SC-002**: TokenFactory generates scoped tokens in <50ms with 99.9% availability
- **SC-003**: Master token cannot access API endpoints (security validation required)
- **SC-004**: All constants are used in place of magic strings (code review validation)
- **SC-005**: Subdomain routing resolves correctly for all services (api.io.proxy, api.io.common, api.io.cass, api.io.elsa)
- **SC-006**: Only proxy service has public access enabled (security audit validation)
- **SC-007**: Scoped tokens expire correctly after configured lifetime (functional test validation)
- **SC-008**: Cryptographic signature validation rejects tampered tokens (security test validation)
- **SC-009**: Health and Swagger endpoints bypass authentication (functional test validation)
- **SC-010**: Environment variables follow nested pattern under api deployment type (YAML validation)

### Observability Requirements (Per Constitution Principle IV)

- **[ ] Authentication Metrics**: Metrics for token validation success/failure rates, average validation time, and scope distribution
- **[ ] TokenFactory Metrics**: Metrics for token generation count, average generation time, and validation results
- **[ ] Security Monitoring**: Alerts for master token access attempts, high failure rates, and unusual token patterns
- **[ ] Performance Baselines**: Token validation must complete in <10ms, token generation in <50ms
- **[ ] Health Endpoints**: TokenFactory service exposes /health endpoint with dependency status (environment variable availability)
- **[ ] Error Tracking**: Structured logging for all authentication failures with correlation IDs, token IDs (hashed), and error categorization
- **[ ] Audit Trail**: Logging of all scoped token generation events with requester ID, scopes granted, and generation timestamp

## Assumptions

- Master token will be generated once and stored securely in Octopus/Kubernetes secrets as TOKEN_FACTORY__MASTER_TOKEN
- Factory secret will be configured via TOKEN_FACTORY__SECRET environment variable (different per environment)
- Default token lifetime of 1 hour is acceptable for HappyRobot and testing scenarios
- Python authentication pattern (checking if token is in scopes list) is sufficient for initial implementation
- MSAL integration is prepared for future OAuth/OIDC upgrade but not required for initial deployment
- Environment variable naming follows USXpress double-underscore convention (TOKEN_FACTORY__MASTER_TOKEN)
- Subdomain routing will be configured via DX platform with proper DNS and ingress setup
- Internal services will communicate via API-to-API flow through the proxy using assigned_to_apps configuration
