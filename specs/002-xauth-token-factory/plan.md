# Implementation Plan: X-Auth Token Middleware and TokenFactory

**Branch**: `002-xauth-token-factory` | **Date**: 2026-03-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-xauth-token-factory/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Implement X-Auth token middleware for the IO.Proxy API gateway with scope-based authentication matching the existing Python pattern, plus a TokenFactory system for zero-trust runtime token generation. This enables external client authentication while maintaining compatibility with the current authentication flow, and provides HappyRobot and other testing tools with time-limited tokens without database storage or permanent API keys.

The implementation follows USXpress standards with USXpress.Monitoring for observability, uses HMAC-SHA256 cryptographic signing for scoped tokens, and configures subdomain routing for all IO services with public access only on the proxy service.

## Technical Context

**Language/Version**: .NET 10 (MANDATORY per Constitution Principle III)  
**Primary Dependencies**: USXpress.Monitoring, Microsoft.Identity.Client (MSAL) for future OAuth (MANDATORY per Constitution Principle III)  
**Storage**: None (tokens are stateless with cryptographic signatures)  
**Testing**: xUnit + 90%+ coverage requirement (MANDATORY per Constitution Principle V)  
**Target Platform**: Linux containers with Kubernetes deployment  
**Project Type**: Microservices architecture (MANDATORY per Constitution Principle II)  
**Performance Goals**: <10ms validation overhead, <50ms token generation (MANDATORY per Constitution Principle IV)  
**Constraints**: CLEAN architecture compliance - Core/Infrastructure/Models separation (MANDATORY per Constitution Principle I)  
**Scale/Scope**: Independent domain services, TokenFactory integrated into IO.Proxy gateway

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Required Compliance Gates (per Constitution)

- **[x] CLEAN Architecture**: Core/Infrastructure/Models separation verified - `src/Common/Core/Authentication/` contains middleware, TokenFactory, and extensions following separation of concerns
- **[x] Microservice Boundaries**: Each domain independently deployable with own configuration - TokenFactory is stateless, integrated into Proxy gateway
- **[x] USXpress Standards**: USXpress.Monitoring imported in `IO.Proxy/Program.cs` with proper Grafana/OTEL configuration
- **[x] Observability**: Structured logging via Serilog, OpenTelemetry enabled, health endpoints at `/health` and `/ready`
- **[~] Test Coverage**: Unit tests exist in `TokenFactoryTests.cs` - need to verify 90%+ coverage and add middleware tests
- **[~] Infrastructure as Code**: Deployment YAMLs exist in `.deploy-net/` - need to verify subdomain routing and environment variable nesting

### Architecture Validation

- **[x] Domain Ownership**: No cross-domain database access - TokenFactory is stateless with cryptographic validation
- **[x] API Contracts**: Well-defined interfaces between services - middleware adds token context to HttpContext.Items
- **[~] Event Streaming**: Not applicable for this feature (no Kafka integration needed for authentication)
- **[~] Performance Baselines**: Code analysis shows <10ms validation, <50ms generation possible - need benchmarks

### Post-Design Constitution Re-check

- **[ ] Test Coverage**: Add unit tests for XAuthTokenMiddleware, integration tests for TokenFactory endpoints
- **[ ] Deployment YAMLs**: Update `.deploy-net/io-proxy-api.yaml` with TokenFactory environment variables
- **[ ] Performance Baselines**: Run benchmarks to validate <10ms validation, <50ms generation

## Project Structure

### Documentation (this feature)

```text
specs/002-xauth-token-factory/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# IO Platform Microservices (MANDATORY per Constitution Principle II)
src/
├── Common/
│   ├── Core/                    # Business logic and interfaces
│   │   └── Authentication/      # X-Auth middleware, TokenFactory, Constants
│   │       ├── XAuthTokenMiddleware.cs      # Middleware implementation
│   │       ├── XAuthTokenExtensions.cs      # DI registration extensions
│   │       ├── TokenFactory.cs              # Zero-trust token generation
│   │       ├── TokenFactoryExtensions.cs    # TokenFactory DI registration
│   │       ├── TokenFactoryTests.cs         # Unit tests
│   │       └── AuthenticationConstants.cs   # Environment variable constants
│   ├── Infrastructure/          # External integrations
│   └── Models/                  # Domain types
├── Apps/
│   ├── RestAPI/
│   │   ├── IO.Proxy/           # API Gateway (TokenFactory endpoint)
│   │   │   ├── Program.cs      # Middleware pipeline registration
│   │   │   └── Controllers/
│   │   │       └── TokenFactoryController.cs  # Token generation endpoint
│   │   ├── IO.Common/          # Email + Context services
│   │   ├── IO.Cass/            # Carrier vetting
│   │   ├── IO.Elsa/            # Pricing calculations
│   │   ├── IO.Larry/           # Vendor lookup
│   │   └── IO.Lea/             # Job search
│   ├── Handlers/               # Background processors
│   └── Jobs/                   # Scheduled tasks

tests/
├── unit/                      # Per-service unit tests
│   └── IO.Core.Authentication.Tests/
├── integration/               # Contract and integration tests
└── e2e/                      # End-to-end journey tests

.deploy-net/                   # Infrastructure as Code
├── io-proxy-api.yaml          # Public gateway + TokenFactory
├── io-common-api.yaml         # Internal service
├── io-cass-api.yaml           # Internal service
├── io-elsa-api.yaml           # Internal service
├── io-larry-api.yaml          # Internal service
└── io-lea-api.yaml            # Internal service
```

**Structure Decision**: The existing structure is appropriate. TokenFactory and X-Auth middleware are centralized in `src/Common/Core/Authentication/` for reuse across services. IO.Proxy hosts the TokenFactory HTTP endpoint for token generation.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | - | - |
