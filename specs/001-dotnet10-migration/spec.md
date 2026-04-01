# Feature Specification: .NET 10 Migration with CLEAN Architecture

**Feature Branch**: `001-dotnet10-migration`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "Migrate Python monolith to .NET 10 with CLEAN architecture following edi-platform patterns, creating microservices for IO.Proxy, IO.Common, IO.Cass, IO.Elsa, IO.Larry, and IO.Lea domains with USXpress infrastructure integration. Include Dockerfile, docker-compose, and deployment YAMLs organized under dotnet/ folder structure."

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

### User Story 1 - API Gateway Implementation (Priority: P1)

As a system architect, I want to implement the IO.Proxy API gateway that routes requests to appropriate domain services, so that clients have a single entry point for all IO platform functionality.

**Why this priority**: The API gateway is foundational for all other services and enables independent deployment of domain microservices while maintaining a unified external interface.

**Independent Test**: Can be fully tested by configuring routing rules and verifying requests are properly forwarded to mock downstream services with correct authentication and monitoring.

**Acceptance Scenarios**:

1. **Given** the IO.Proxy service is running, **When** a client requests `/api/common/email`, **Then** the request is routed to IO.Common service with proper authentication headers
2. **Given** downstream service is unavailable, **When** a request is made, **Then** the gateway returns appropriate error response with monitoring metrics
3. **Given** multiple requests are made, **When** load testing is performed, **Then** the gateway handles 1000+ concurrent requests without degradation

---

### User Story 2 - Common Services Migration (Priority: P1)

As a user, I want the email and context services to be migrated to .NET 10, so that I can send emails and manage user context through the new platform with the same functionality as the Python version.

**Why this priority**: Common services are used by multiple domains and must be available before other domain services can be migrated.

**Independent Test**: Can be fully tested by sending email requests and context operations, verifying SendGrid integration and MongoDB data persistence work correctly.

**Acceptance Scenarios**:

1. **Given** IO.Common service is deployed, **When** an email request is submitted, **Then** the email is sent via SendGrid and response status is returned
2. **Given** user context data exists, **When** context is requested, **Then** the correct context is retrieved from MongoDB
3. **Given** invalid email data, **When** send request is made, **Then** appropriate error response is returned with validation details

---

### User Story 3 - Carrier Vetting Service Migration (Priority: P2)

As a carrier compliance officer, I want the carrier vetting service migrated to .NET 10, so that I can validate carrier eligibility using Highway and Mcleod APIs with the same business rules as the current system.

**Why this priority**: Carrier vetting is critical for business operations and has complex integration requirements that need validation.

**Independent Test**: Can be fully tested by submitting carrier validation requests and verifying the integration with Highway and Mcleod APIs produces correct validity responses.

**Acceptance Scenarios**:

1. **Given** valid carrier DOT/MC numbers, **When** validation is requested, **Then** the service returns valid status with carrier contacts
2. **Given** carrier fails compliance rules, **When** validation is requested, **Then** appropriate error codes and failure reasons are returned
3. **Given** external API is unavailable, **When** validation is requested, **Then** graceful degradation with proper error handling occurs

---

### User Story 4 - Background Processing Migration (Priority: P2)

As a system administrator, I want vendor lookup and job search background jobs migrated to .NET 10 Worker Services, so that scheduled tasks continue to function with proper Kafka integration and monitoring.

**Why this priority**: Background processing is essential for data synchronization and must work reliably before full migration.

**Independent Test**: Can be fully tested by running the worker services and verifying Kafka message consumption and data updates occur as expected.

**Acceptance Scenarios**:

1. **Given** Kafka messages are published, **When** worker services are running, **Then** messages are consumed and processed correctly
2. **Given** scheduled job triggers, **When** execution time is reached, **Then** jobs run and update data as expected
3. **Given** processing errors occur, **When** exceptions happen, **Then** errors are logged and monitoring alerts are triggered

---

### Edge Cases

- What happens when external APIs (Highway, Mcleod, SendGrid) are rate limited or unavailable?
- How does system handle MongoDB connection failures or TLS certificate issues?
- What occurs when Kafka consumer groups are lagging or topics are not available?
- How does the system handle authentication token expiration or invalid tokens?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST provide API gateway that routes requests to appropriate domain microservices with load balancing
- **FR-002**: System MUST implement email service using SendGrid with HTML template rendering and signature support  
- **FR-003**: System MUST provide user context management with MongoDB persistence (Genesys integration deferred to post-migration phase)
- **FR-004**: System MUST implement carrier vetting service with Highway and Mcleod API integration following existing business rules
- **FR-005**: System MUST provide pricing calculation service with external API integration and cost breakdown
- **FR-006**: System MUST implement vendor lookup service with geographic search and scheduled data synchronization
- **FR-007**: System MUST provide job search service with geographic polygon matching and Google Jobs integration
- **FR-008**: System MUST use MongoDB Atlas for data persistence with TLS authentication and proper connection pooling
- **FR-009**: System MUST implement Kafka message consumption for background processing with proper consumer groups
- **FR-010**: System MUST include structured logging, OpenTelemetry tracing, and Grafana metrics for observability
- **FR-011**: System MUST authenticate requests via X-Auth token middleware with scope-based validation following USXpress Azure AD patterns
- **FR-012**: System MUST integrate existing TokenFactory from 002-xauth-token-factory for zero-trust runtime token generation using environment-based master token
- **FR-013**: System MUST support graceful shutdown and health endpoints for all services
- **FR-014**: System MUST deploy using Docker containers with multi-stage builds and environment-specific configuration
- **FR-015**: System MUST organize all .NET migration artifacts under dotnet/ folder structure with proper separation
- **FR-016**: System MUST provide subdomain routing via api.io.{service} pattern with public access only on proxy

### Key Entities

- **EmailRequest**: Represents email sending request with recipients, content, and formatting options
- **UserContext**: Represents user profile and preference data stored in MongoDB for personalization
- **CarrierValidation**: Represents carrier eligibility assessment with contacts and compliance status
- **PricingCalculation**: Represents load pricing breakdown with distance, base cost, and surcharges
- **Vendor**: Represents service provider with location, contact information, and availability
- **JobPosting**: Represents employment opportunity with location, requirements, and posting details
- **ServiceHealth**: Represents microservice health status with dependencies and performance metrics
- **XAuthToken**: Represents authentication token with scope validation for API access control
- **TokenFactory**: Represents zero-trust token generation system with master token and scoped token lifecycle

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  Per Constitution Principle IV: All criteria must be observable via monitoring/telemetry.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: API gateway routes 1000+ concurrent requests with <100ms average response time
- **SC-002**: Email service achieves 99.9% delivery success rate with SendGrid integration
- **SC-003**: Carrier vetting service processes validations in <2 seconds with 95% accuracy
- **SC-004**: All services maintain 99.9% uptime with proper health monitoring and alerting
- **SC-005**: Background processing achieves <5 minute message processing lag for Kafka events
- **SC-006**: System completes full migration from Python to .NET 10 with zero data loss
- **SC-007**: All services pass security and compliance audits with proper authentication and data protection
- **SC-008**: X-Auth token middleware validates 100% of requests with <10ms overhead per request
- **SC-009**: TokenFactory generates scoped tokens in <50ms with 99.9% availability
- **SC-010**: Master token cannot access API endpoints (security validation required)

### Observability Requirements (Per Constitution Principle IV)

- **[ ] Performance Baselines**: Metrics established before each service rollout with response time, error rate, and throughput targets
- **[ ] Health Endpoints**: All services expose /health and /ready endpoints with dependency status
- **[ ] Error Tracking**: Structured logging for all failures with correlation IDs and error categorization
- **[ ] User Journey Metrics**: End-to-end request tracing across all microservices with timing data

## Assumptions

- Existing Python service APIs and business rules will be preserved exactly during migration
- MongoDB Atlas clusters and Kafka topics are already provisioned and accessible
- SendGrid API keys and external service credentials are available in target environment
- Azure AD authentication configuration is already established for the organization
- Development team has .NET 10 development environment and USXpress NuGet package access
- Docker container registry and Kubernetes deployment infrastructure are available
- External APIs (Highway, Mcleod, pricing services) will maintain current contracts during migration
