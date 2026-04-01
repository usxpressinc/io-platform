---

description: "Task list for .NET 10 Migration with CLEAN Architecture"
---

# Tasks: .NET 10 Migration with CLEAN Architecture

**Input**: Design documents from `/specs/001-dotnet10-migration/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are OPTIONAL - only include test tasks if explicitly requested in feature specification or if TDD approach is requested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **.NET microservices**: `dotnet/src/`, `dotnet/tests/` at repository root
- **CLEAN Architecture**: Core/, Infrastructure/, Models/, Apps/ structure
- Paths follow plan.md structure with dotnet/ folder organization

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create dotnet/ folder structure per implementation plan in plan.md
- [ ] T002 Initialize .NET 10 solution file io-platform.sln with CLEAN architecture projects
- [ ] T003 [P] Configure Directory.Build.props and Directory.Packages.props for centralized package management
- [ ] T004 [P] Setup Dockerfile with multi-stage build for .NET 10
- [ ] T005 [P] Create docker-compose.yaml for local development with all services
- [ ] T006 [P] Configure .gitignore for .NET build artifacts and secrets
- [ ] T007 [P] Configure subdomain routing api.io.{service} pattern in dotnet/.octopus/deploy/ YAMLs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T008 Setup USXpress.Monitoring package integration in dotnet/Directory.Packages.props
- [ ] T009 [P] Setup USXpress.Configuration.Mongo package for MongoDB Atlas integration with TLS
- [ ] T010 [P] Setup USXpress.Kafka package for message consumption with consumer groups
- [ ] T011 [P] Configure structured logging and OpenTelemetry in dotnet/src/Common/Core/Monitoring/
- [ ] T012 [P] Create base health endpoints structure in dotnet/src/Common/Core/Health/
- [ ] T013 [P] Setup environment configuration management with .env support
- [ ] T014 [P] Integrate existing XAuthTokenFactory from 002-xauth-token-factory in dotnet/src/Common/Core/Authentication/
- [ ] T015 [P] Create base cross-domain entities (ServiceHealth, XAuthToken) in dotnet/src/Common/Models/
- [ ] T016 [P] Setup error handling and exception middleware structure in dotnet/src/Common/Core/Exceptions/
- [ ] T017 [P] Configure MongoDB connection pooling and TLS certificate handling
- [ ] T018 [P] Implement comprehensive graceful shutdown patterns in dotnet/src/Common/Core/Lifecycle/GracefulShutdownService.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - API Gateway Implementation (Priority: P1) 🎯 MVP

**Goal**: Implement IO.Proxy API gateway that routes requests to appropriate domain services with single entry point

**Independent Test**: Can be fully tested by configuring routing rules and verifying requests are properly forwarded to mock downstream services with correct authentication and monitoring

### Implementation for User Story 1

- [ ] T019 [P] [US1] Create IO.Proxy project structure in dotnet/src/Apps/RestAPI/IO.Proxy/
- [ ] T020 [P] [US1] Implement routing configuration in dotnet/src/Apps/RestAPI/IO.Proxy/Core/Routing/
- [ ] T021 [P] [US1] Create HTTP client factory for downstream service communication in dotnet/src/Apps/RestAPI/IO.Proxy/Infrastructure/Http/
- [ ] T022 [US1] Implement load balancing and service discovery in dotnet/src/Apps/RestAPI/IO.Proxy/Core/LoadBalancing/
- [ ] T023 [US1] Add request/response transformation middleware in dotnet/src/Apps/RestAPI/IO.Proxy/Middleware/
- [ ] T024 [US1] Configure authentication forwarding in dotnet/src/Apps/RestAPI/IO.Proxy/Core/Authentication/
- [ ] T025 [US1] Implement health check aggregation in dotnet/src/Apps/RestAPI/IO.Proxy/Core/Health/
- [ ] T026 [US1] Create monitoring and metrics collection in dotnet/src/Apps/RestAPI/IO.Proxy/Core/Monitoring/
- [ ] T027 [US1] Setup API gateway endpoints in dotnet/src/Apps/RestAPI/IO.Proxy/Program.cs
- [ ] T028 [US1] Add graceful shutdown and readiness probes in dotnet/src/Apps/RestAPI/IO.Proxy/Program.cs
- [ ] T029 [US1] Create deployment YAML with api.io.proxy subdomain routing in dotnet/.octopus/deploy/io-proxy-api.yaml

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Common Services Migration (Priority: P1)

**Goal**: Migrate email and context services to .NET 10 with SendGrid integration and MongoDB persistence

**Independent Test**: Can be fully tested by sending email requests and context operations, verifying SendGrid integration and MongoDB data persistence work correctly

### Implementation for User Story 2

- [ ] T028 [P] [US2] Create IO.Common project structure in dotnet/src/Apps/RestAPI/IO.Common/
- [ ] T029 [P] [US2] Create EmailRequest model in dotnet/src/Common/Models/Email/EmailRequest.cs
- [ ] T030 [P] [US2] Create UserContext model in dotnet/src/Common/Models/Users/UserContext.cs
- [ ] T031 [P] [US2] Create UserPreferences nested model in dotnet/src/Common/Models/Users/UserPreferences.cs
- [ ] T032 [P] [US2] Implement SendGrid email service in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Email/SendGridService.cs
- [ ] T033 [P] [US2] Create MongoDB repositories for email logs in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Data/EmailRepository.cs
- [ ] T034 [P] [US2] Create MongoDB repositories for user contexts in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Data/UserContextRepository.cs
- [ ] T035 [P] [US2] Implement email business logic in dotnet/src/Apps/RestAPI/IO.Common/Core/Email/EmailService.cs
- [ ] T036 [P] [US2] Implement user context business logic in dotnet/src/Apps/RestAPI/IO.Common/Core/Users/UserContextService.cs
- [ ] T037 [US2] Create email endpoints in dotnet/src/Apps/RestAPI/IO.Common/Endpoints/EmailEndpoints.cs
- [ ] T038 [US2] Create user context endpoints in dotnet/src/Apps/RestAPI/IO.Common/Endpoints/UserContextEndpoints.cs
- [ ] T039 [US2] Add validation and error handling for email requests in dotnet/src/Apps/RestAPI/IO.Common/Core/Validation/
- [ ] T040 [US2] Add logging for email and context operations in dotnet/src/Apps/RestAPI/IO.Common/Core/Logging/
- [ ] T041 [US2] Setup IO.Common service configuration in dotnet/src/Apps/RestAPI/IO.Common/Program.cs
- [ ] T042 [US2] Create deployment YAML with api.io.common subdomain routing in dotnet/.octopus/deploy/io-common-api.yaml

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Carrier Vetting Service Migration (Priority: P2)

**Goal**: Migrate carrier vetting service to .NET 10 with Highway and Mcleod API integration

**Independent Test**: Can be fully tested by submitting carrier validation requests and verifying the integration with Highway and Mcleod APIs produces correct validity responses

### Implementation for User Story 3

- [ ] T043 [P] [US3] Create IO.Cass project structure in dotnet/src/Apps/RestAPI/IO.Cass/
- [ ] T044 [P] [US3] Create CarrierValidation model in dotnet/src/Common/Models/Carriers/CarrierValidation.cs
- [ ] T045 [P] [US3] Create CarrierContact nested model in dotnet/src/Common/Models/Carriers/CarrierContact.cs
- [ ] T046 [P] [US3] Create HighwayResponse model in dotnet/src/Common/Models/Carriers/HighwayResponse.cs
- [ ] T047 [P] [US3] Create McleodResponse model in dotnet/src/Common/Models/Carriers/McleodResponse.cs
- [ ] T048 [P] [US3] Implement Highway API client in dotnet/src/Apps/RestAPI/IO.Cass/Infrastructure/External/HighwayApiClient.cs
- [ ] T049 [P] [US3] Implement Mcleod API client in dotnet/src/Apps/RestAPI/IO.Cass/Infrastructure/External/McleodApiClient.cs
- [ ] T050 [P] [US3] Create MongoDB repository for carrier validations in dotnet/src/Apps/RestAPI/IO.Cass/Infrastructure/Data/CarrierValidationRepository.cs
- [ ] T051 [P] [US3] Implement carrier vetting business logic in dotnet/src/Apps/RestAPI/IO.Cass/Core/Carriers/CarrierVettingService.cs
- [ ] T052 [P] [US3] Add validation rules and compliance checks in dotnet/src/Apps/RestAPI/IO.Cass/Core/Validation/CarrierValidationRules.cs
- [ ] T053 [US3] Create carrier validation endpoints in dotnet/src/Apps/RestAPI/IO.Cass/Endpoints/CarrierValidationEndpoints.cs
- [ ] T054 [US3] Add circuit breaker pattern for external API failures in dotnet/src/Apps/RestAPI/IO.Cass/Infrastructure/Resilience/
- [ ] T055 [US3] Add logging for carrier vetting operations in dotnet/src/Apps/RestAPI/IO.Cass/Core/Logging/
- [ ] T056 [US3] Setup IO.Cass service configuration in dotnet/src/Apps/RestAPI/IO.Cass/Program.cs
- [ ] T057 [US3] Create deployment YAML with api.io.cass subdomain routing in dotnet/.octopus/deploy/io-cass-api.yaml

**Checkpoint**: User Story 3 should now be independently functional

---

## Phase 6: User Story 4 - Background Processing Migration (Priority: P2)

**Goal**: Migrate vendor lookup and job search background jobs to .NET 10 Worker Services with Kafka integration

**Independent Test**: Can be fully tested by running the worker services and verifying Kafka message consumption and data updates occur as expected

### Implementation for User Story 4

- [ ] T058 [P] [US4] Create IO.Elsa project structure in dotnet/src/Apps/RestAPI/IO.Elsa/
- [ ] T059 [P] [US4] Create IO.Larry project structure in dotnet/src/Apps/RestAPI/IO.Larry/
- [ ] T060 [P] [US4] Create IO.Lea project structure in dotnet/src/Apps/RestAPI/IO.Lea/
- [ ] T061 [P] [US4] Create PricingCalculation model in dotnet/src/Common/Models/Pricing/PricingCalculation.cs
- [ ] T062 [P] [US4] Create Vendor model in dotnet/src/Common/Models/Vendors/Vendor.cs
- [ ] T063 [P] [US4] Create JobPosting model in dotnet/src/Common/Models/Jobs/JobPosting.cs
- [ ] T064 [P] [US4] Implement Kafka consumer for vendor-lookup-sync in dotnet/src/Apps/Handlers/VendorLookupHandler.cs
- [ ] T065 [P] [US4] Implement Kafka consumer for job-search-index in dotnet/src/Apps/Handlers/JobSearchHandler.cs
- [ ] T066 [P] [US4] Implement Kafka consumer for email-notifications in dotnet/src/Apps/Handlers/EmailNotificationHandler.cs
- [ ] T067 [P] [US4] Create MongoDB repositories for vendor data in dotnet/src/Apps/RestAPI/IO.Larry/Infrastructure/Data/VendorRepository.cs
- [ ] T068 [P] [US4] Create MongoDB repositories for job postings in dotnet/src/Apps/RestAPI/IO.Lea/Infrastructure/Data/JobPostingRepository.cs
- [ ] T069 [P] [US4] Create MongoDB repositories for pricing calculations in dotnet/src/Apps/RestAPI/IO.Elsa/Infrastructure/Data/PricingRepository.cs
- [ ] T070 [P] [US4] Implement pricing service with external API in dotnet/src/Apps/RestAPI/IO.Elsa/Core/Pricing/PricingService.cs
- [ ] T071 [P] [US4] Implement vendor lookup service in dotnet/src/Apps/RestAPI/IO.Larry/Core/Vendors/VendorService.cs
- [ ] T072 [P] [US4] Implement job search service with Google Jobs integration in dotnet/src/Apps/RestAPI/IO.Lea/Core/Jobs/JobSearchService.cs
- [ ] T073 [P] [US4] Create external API clients (pricing, Google Jobs) in respective Infrastructure/External/ folders
- [ ] T074 [P] [US4] Implement scheduled job infrastructure in dotnet/src/Apps/Jobs/
- [ ] T075 [US4] Create pricing endpoints in dotnet/src/Apps/RestAPI/IO.Elsa/Endpoints/PricingEndpoints.cs
- [ ] T076 [US4] Create vendor endpoints in dotnet/src/Apps/RestAPI/IO.Larry/Endpoints/VendorEndpoints.cs
- [ ] T077 [US4] Create job posting endpoints in dotnet/src/Apps/RestAPI/IO.Lea/Endpoints/JobEndpoints.cs
- [ ] T078 [US4] Add error handling and monitoring for background jobs in dotnet/src/Apps/Handlers/
- [ ] T079 [US4] Setup service configurations in respective Program.cs files
- [ ] T080 [US4] Create deployment YAMLs with api.io.elsa, api.io.larry, api.io.lea subdomain routing in dotnet/.octopus/deploy/

**Checkpoint**: All user stories should now be independently functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T081 [P] Update quickstart.md with actual service URLs and configuration examples
- [ ] T082 [P] Create MongoDB index creation scripts for all collections in dotnet/scripts/mongo-indexes/
- [ ] T083 [P] Add comprehensive error logging with correlation IDs across all services
- [ ] T084 [P] Implement rate limiting for external API calls in all services
- [ ] T085 [P] Add comprehensive unit tests for business logic in dotnet/tests/unit/
- [ ] T086 [P] Add integration tests for API contracts in dotnet/tests/integration/
- [ ] T087 [P] Add end-to-end tests for critical user journeys in dotnet/tests/e2e/
- [ ] T088 [P] Performance optimization and load testing setup
- [ ] T089 [P] Security hardening and vulnerability scanning
- [ ] T090 [P] Documentation updates for API contracts and deployment procedures
- [ ] T091 [P] Validate quickstart.md instructions work end-to-end
- [ ] T092 [P] Create migration scripts for existing Python data to .NET schemas
- [ ] T093 [P] Add security audit preparation tasks for SC-007 compliance in dotnet/scripts/security-audit/
- [ ] T094 [P] Add master token security validation tests for SC-010 in dotnet/tests/security/MasterTokenSecurityTests.cs
- [ ] T095 [P] Add performance baseline establishment tasks in dotnet/scripts/performance-baselines/
- [ ] T096 [P] Add comprehensive health endpoint implementation with dependency status in dotnet/src/Common/Core/Health/
- [ ] T097 [P] Add structured error tracking with correlation IDs in dotnet/src/Common/Core/Logging/
- [ ] T098 [P] Add end-to-end request tracing across microservices in dotnet/src/Common/Core/Tracing/

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Should be independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Should be independently testable
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Should be independently testable

### Within Each User Story

- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Summary

- **Total Tasks**: 99 tasks across 7 phases  
- **User Stories**: 4 stories with independent implementation paths
- **Parallel Opportunities**: 77 tasks marked [P] for parallel execution
- **MVP Scope**: User Stories 1 & 2 (API Gateway + Common Services)

# Launch all infrastructure components together:
Task: "Implement SendGrid email service in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Email/SendGridService.cs"
Task: "Create MongoDB repositories for email logs in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Data/EmailRepository.cs"
Task: "Create MongoDB repositories for user contexts in dotnet/src/Apps/RestAPI/IO.Common/Infrastructure/Data/UserContextRepository.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (API Gateway)
4. Complete Phase 4: User Story 2 (Common Services)
5. **STOP and VALIDATE**: Test both stories independently
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP Gateway!)
3. Add User Story 2 → Test independently → Deploy/Demo (MVP Services!)
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (API Gateway)
   - Developer B: User Story 2 (Common Services)
   - Developer C: User Story 3 (Carrier Vetting)
   - Developer D: User Story 4 (Background Processing)
3. Stories complete and integrate independently

---

## Documentation Reconciliation

### New Documentation Required
- [ ] Update API documentation for all 6 services
- [ ] Create deployment guide for Kubernetes
- [ ] Document MongoDB schema migrations
- [ ] Update architecture diagrams with .NET 10 stack

### Existing Documentation to Update
- [ ] Update README.md with .NET migration information
- [ ] Update development environment setup instructions
- [ ] Document integration with 002-xauth-token-factory
- [ ] Verify no stale Python references remain in documentation

### Documentation Location
- Path: `docs/` folder in repository root
- Format: Markdown with Mermaid diagrams for architecture

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- All services must integrate with existing XAuthTokenFactory from 002-xauth-token-factory
- Follow CLEAN architecture principles (Core/Infrastructure/Models separation)
- All services must use USXpress packages for monitoring, MongoDB, and Kafka
- Zero data loss requirement during migration - preserve existing Python API contracts
- Performance targets from spec must be met (API gateway <100ms, etc.)
