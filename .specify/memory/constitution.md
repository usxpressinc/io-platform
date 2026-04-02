<!--
Sync Impact Report:
Version change: 0.0.0 → 1.0.0 (initial constitution)
Added sections: Core Principles (6 principles), Architecture Standards, Development Workflow, Governance
Templates requiring updates: ✅ All templates aligned with new principles
Follow-up TODOs: None
-->

# IO Platform Constitution

## Core Principles

### I. CLEAN Architecture First
Every domain MUST follow CLEAN architecture with clear separation of concerns: Core (business logic), Infrastructure (external integrations), Models (domain types). Common libraries MUST be independent of application frameworks and independently testable.

### II. Microservice Boundaries
Each domain (Proxy, Common, Cass, Elsa) MUST be independently deployable with its own database and API contract. Cross-domain communication MUST occur through well-defined APIs or event streams, never direct database access.

### III. USXpress Standards Compliance
All services MUST use USXpress standard packages: USXpress.Monitoring for observability, USXpress.Configuration.Mongo for data access, USXpress.Kafka for messaging. Authentication MUST integrate with Azure AD using established patterns.

### IV. Observability Non-Negotiable
Every service MUST include structured logging, OpenTelemetry tracing, and Grafana metrics. All deployments MUST auto-inject monitoring configuration and expose health endpoints. Performance baselines MUST be established before feature rollout.

### V. Test-First Discipline
Unit tests MUST be written before implementation. Integration tests MUST cover external service contracts. End-to-end tests MUST validate critical user journeys. No code MAY be merged without passing all test gates.

### VI. Infrastructure as Code
All deployment configurations MUST be versioned in `.octopus/deploy/` following edi-platform patterns. Environment variables MUST be nested under service types (api, handler, cron). No manual configuration changes allowed.

## Architecture Standards

### Technology Stack
- .NET 10 for all services with centralized package management via Directory.Packages.props
- MongoDB Atlas for data persistence with TLS authentication
- Kafka for event streaming with proper consumer groups
- Docker multi-stage builds following edi-platform patterns
- GitHub Actions with variant-inc actions for CI/CD

### Service Patterns
- APIs use ASP.NET Core with minimal endpoints and Swagger documentation
- Background processors use .NET Worker Services with Kafka integration
- Scheduled tasks use hosted services with proper error handling
- All services support graceful shutdown and health checks

### Data Management
Each domain owns its data with no cross-domain database access. MongoDB collections MUST be defined in constants with proper indexing. Event sourcing patterns MUST be used for audit trails.

## Development Workflow

### Code Quality
All code MUST pass StyleCop analysis and maintain 90%+ test coverage. Pull requests require at least one technical review. Performance testing required for API changes.

### Branching Strategy
Feature branches follow pattern `###-feature-name`. Main branch represents deployable state. Release branches created for major deployments with proper versioning.

### Documentation
Every feature MUST include specification in `.specify/specs/` with measurable success criteria. Architecture decisions MUST be documented in ADRs. API documentation MUST be current and accurate.

## Governance

### Constitution Authority
This constitution supersedes all conflicting practices. All team members MUST ensure compliance during reviews and implementations.

### Amendment Process
Constitution amendments require:
1. Proposal with impact analysis
2. Team review and discussion
3. Supermajority approval (75%+)
4. Documentation update
5. Migration plan for existing code

### Compliance Review
Monthly compliance audits MUST be conducted. Non-compliance MUST be addressed within 2 weeks. Technical debt MUST be tracked and prioritized in backlog.

**Version**: 1.0.0 | **Ratified**: 2026-03-31 | **Last Amended**: 2026-03-31
