# Tasks: X-Auth Token Middleware and TokenFactory

**Input**: Design documents from `/specs/002-xauth-token-factory/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

**Tests**: **REQUIRED per Constitution Principle V** - Unit tests must be written before implementation. Test tasks included below.

**Note**: Per Test-First Discipline, all test tasks marked with ⚠️ MUST be completed and verified FAILING before corresponding implementation tasks begin.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure verification

- [ ] T001 Verify .NET 10 SDK is installed (`dotnet --version` returns 10.x)
- [ ] T002 Verify USXpress.Monitoring package reference exists in `src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj`
- [ ] T003 [P] Verify Microsoft.Identity.Client package reference exists for future OAuth support

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 [P] Create `AuthenticationConstants.cs` with environment variable constants in `src/Common/Core/Constants/`
- [x] T005 [P] Create `ServiceEndpoints.cs` with service endpoint constants in `src/Common/Core/Constants/`
- [x] T006 [P] Create `MongoDbCollections.cs` with collection name constants in `src/Common/Core/Constants/`
- [x] T007 [P] Create `KafkaTopics.cs` with topic name constants in `src/Common/Core/Constants/`
- [x] T008 Create `TokenFactoryOptions.cs` configuration class in `src/Common/Core/Authentication/`
- [x] T009 Create `XAuthTokenOptions.cs` configuration class in `src/Common/Core/Authentication/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - X-Auth Token Middleware (Priority: P1) 🎯 MVP

**Goal**: Implement X-Auth token middleware for IO.Proxy API gateway with scope-based authentication matching Python pattern

**Independent Test**: Configure middleware with test tokens and verify requests with valid tokens are accepted while invalid tokens are rejected with appropriate HTTP status codes (401, 403)

### Tests for User Story 1 ⚠️ (Write First - Must Fail Before Implementation)

- [x] T010a [P] [US1] Create `XAuthTokenMiddlewareTests.cs` with test for valid Bearer token acceptance (should fail initially)
- [x] T010b [P] [US1] Create test for missing Authorization header returns 401 (should fail initially)
- [x] T010c [P] [US1] Create test for invalid scope returns 403 (should fail initially)
- [x] T010d [P] [US1] Create test for health endpoint bypass (should fail initially)
- [x] T010e [P] [US1] Create test for X-Auth-Token header support (should fail initially)

### Implementation for User Story 1

- [x] T010 [P] Create `TokenValidationResult.cs` model in `src/Common/Core/Authentication/`
- [x] T011 [P] Update `XAuthTokenMiddleware.cs` header extraction logic for Bearer and X-Auth-Token headers in `src/Common/Core/Authentication/`
- [x] T012 [P] Implement path-based scope mapping in `XAuthTokenMiddleware.cs` for /api/common, /api/cass, /api/elsa routes
- [x] T013 Update `XAuthTokenMiddleware.cs` token validation logic with scope checking (token in scopes pattern)
- [x] T014 Update `XAuthTokenMiddleware.cs` bypass authentication for /health, /ready, /swagger paths
- [x] T015 Update `XAuthTokenMiddleware.cs` structured JSON error responses for 401 and 403
- [x] T016 Update `XAuthTokenMiddleware.cs` add HttpContext.Items for XAuthToken, UserId, Scopes
- [x] T017 Update `XAuthTokenExtensions.cs` DI registration with IConfiguration binding in `src/Common/Core/Authentication/`
- [x] T018 Update `IO.Proxy/Program.cs` to add `UseXAuthTokenAuthentication()` in middleware pipeline
- [x] T019 Update `IO.Proxy/Program.cs` to configure environment variables for TokenFactory

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - TokenFactory for Zero-Trust (Priority: P1)

**Goal**: Implement TokenFactory system using environment-variable-based master token to generate scoped tokens at runtime

**Independent Test**: Configure TokenFactory with master token from environment and verify: (1) master token cannot access API endpoints, (2) scoped tokens can be generated, (3) scoped tokens expire correctly, (4) token validation works with cryptographic signatures

### Tests for User Story 2 ⚠️ (Write First - Must Fail Before Implementation)

- [x] T020a [P] [US2] Create `TokenFactoryTests.cs` with test for master token validation (type="master") (should fail initially)
- [x] T020b [P] [US2] Create test for scoped token generation with valid master token (should fail initially)
- [x] T020c [P] [US2] Create test for master token cannot access API endpoints (should fail initially)
- [x] T020d [P] [US2] Create test for token expiration validation (should fail initially)
- [x] T020e [P] [US2] Create test for HMAC signature verification rejects tampered tokens (should fail initially)
- [x] T020f [P] [US2] Create test for token validation endpoint (should fail initially)

### Implementation for User Story 2

- [x] T020 [P] Create `ScopedTokenData.cs` internal model in `src/Common/Core/Authentication/`
- [x] T021 [P] Create `MasterTokenData.cs` internal model in `src/Common/Core/Authentication/`
- [x] T022 [P] Create `ScopedTokenResult.cs` public result model in `src/Common/Core/Authentication/`
- [x] T023 Update `TokenFactory.cs` master token validation with type="master" check in `src/Common/Core/Authentication/`
- [x] T024 Update `TokenFactory.cs` scoped token generation with HMAC-SHA256 signing
- [x] T025 Update `TokenFactory.cs` `ValidateScopedToken()` method with signature verification and expiration check
- [x] T026 Update `TokenFactory.cs` `GenerateMasterToken()` static method for initial setup
- [x] T027 Update `TokenFactory.cs` `GenerateAllServiceTokens()` for HappyRobot/testing tools
- [x] T028 Update `TokenFactoryExtensions.cs` DI registration with environment variable configuration in `src/Common/Core/Authentication/`
- [x] T029 Create `TokenFactoryController.cs` with POST `/api/tokens/generate` endpoint in `src/Apps/RestAPI/IO.Proxy/Controllers/`
- [x] T030 Create `TokenFactoryController.cs` with POST `/api/tokens/generate-all` endpoint
- [x] T031 Create `TokenFactoryController.cs` with POST `/api/tokens/validate` endpoint
- [x] T032 Create `TokenFactoryController.cs` master token blocking for API access (security requirement)
- [x] T033 Update `IO.Proxy/Program.cs` add TokenFactory DI services

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Constants Pattern Implementation (Priority: P2)

**Goal**: Implement centralized constants classes for MongoDB collections, Kafka topics, environment variables, service endpoints, and authentication scopes

**Independent Test**: Reference constants classes in code and verify compilation succeeds and all expected constants are defined with correct values

### Implementation for User Story 3

- [x] T034 [US3] Update `AuthenticationConstants.cs` add environment variable constants for AUTH__API_TOKEN, AUTH__JWT_SECRET, AUTH__CLIENT_SECRET in `src/Common/Core/Constants/`
- [x] T035 [US3] Update `AuthenticationConstants.cs` add TokenFactory environment variable constants TOKEN_FACTORY__SECRET, TOKEN_FACTORY__MASTER_TOKEN, TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS
- [x] T036 [US3] Update `AuthenticationConstants.cs` add skip authentication paths constants for /health, /ready, /swagger
- [x] T037 [US3] Update `ServiceEndpoints.cs` add service endpoint constants for Common, Cass, Elsa services
- [x] T038 [US3] Update `MongoDbCollections.cs` add collection name constants per data model requirements
- [x] T039 [US3] Update `KafkaTopics.cs` add topic name constants per messaging requirements
- [x] T040 [US3] Update `AuthenticationScopes.cs` add scope constants for io-proxy-reader, io-proxy-writer, io-common-reader, io-common-writer, io-cass-reader, io-cass-writer, io-elsa-reader, io-elsa-writer
- [x] T041 [US3] Update `IO.Proxy/Program.cs` replace hardcoded strings with constants
- [x] T042 [US3] Update `.deploy-net/io-proxy-api.yaml` add TokenFactory secretVars: `TOKEN_FACTORY__SECRET: '#{TOKEN_FACTORY__SECRET}'` and `TOKEN_FACTORY__MASTER_TOKEN: '#{TOKEN_FACTORY__MASTER_TOKEN}'`
- [x] T043 [US3] Update `.deploy-net/io-proxy-api.yaml` add TokenFactory configVars: `TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS: '#{TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS}'`

**Checkpoint**: All constants implemented and hardcoded strings replaced

---

## Phase 6: User Story 4 - Subdomain Routing Configuration (Priority: P2)

**Goal**: Configure subdomain routing for all IO services with public access only on proxy service

**Independent Test**: Deploy configuration and verify DNS resolution and routing work correctly for each subdomain, with proxy being the only public endpoint

### Implementation for User Story 4

- [x] T044 [US4] Update `.deploy-net/io-proxy-api.yaml` verify `public.enabled: true` for external access
- [x] T045 [US4] Update `.deploy-net/io-proxy-api.yaml` verify subdomain routing `product: io`, `app: proxy`, `type: api`
- [x] T046 [US4] Update `.deploy-net/io-common-api.yaml` verify subdomain routing `product: io`, `app: common`, `type: api`
- [x] T047 [US4] Update `.deploy-net/io-common-api.yaml` remove `public.enabled` (internal service)
- [x] T048 [US4] Update `.deploy-net/io-cass-api.yaml` verify subdomain routing `product: io`, `app: cass`, `type: api`
- [x] T049 [US4] Update `.deploy-net/io-cass-api.yaml` remove `public.enabled` (internal service)
- [x] T050 [US4] Update `.deploy-net/io-elsa-api.yaml` verify subdomain routing `product: io`, `app: elsa`, `type: api`
- [x] T051 [US4] Update `.deploy-net/io-elsa-api.yaml` remove `public.enabled` (internal service)
- [x] T056 [US4] Update `.deploy-net/io-proxy-api.yaml` verify `assigned_to_apps` configuration for API-to-API flow to downstream services

**Checkpoint**: All subdomain routing configured correctly

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Performance Baseline Tasks (Constitution Principle IV Compliance)

- [ ] T065 [P] Create benchmark test for token validation <10ms (SC-001) in `tests/performance/TokenValidationBenchmarks.cs`
- [ ] T066 [P] Create benchmark test for token generation <50ms (SC-002) in `tests/performance/TokenGenerationBenchmarks.cs`
- [ ] T067 Run performance benchmarks and establish baselines before rollout

### Observability Tasks (Constitution Principle IV Compliance)

- [ ] T057 [P] Add structured logging for token validation events with correlation IDs in `XAuthTokenMiddleware.cs`
- [ ] T058 [P] Add audit logging for token generation events with requester ID in `TokenFactoryController.cs`
- [ ] T059 [P] Add authentication metrics (validation success/failure rates) via USXpress.Monitoring
- [ ] T060 [P] Add TokenFactory metrics (generation count, average time) via USXpress.Monitoring
- [ ] T068 [P] Add security monitoring alerts for master token access attempts
- [ ] T069 [P] Add security monitoring alerts for high failure rates and unusual token patterns
- [ ] T070 [P] Implement error tracking with correlation IDs and hashed token IDs
- [ ] T071 Verify TokenFactory /health endpoint exposes dependency status (env var availability)
- [ ] T061 Update `quickstart.md` with local development setup instructions
- [ ] T062 Update `contracts/authentication-flow.md` with updated Mermaid diagrams if needed
- [ ] T063 Run `dotnet build` to verify all projects compile without errors
- [ ] T064 Run quickstart.md validation steps for token generation and authentication flows
- [ ] T072 Run all unit tests and verify 90%+ coverage (per Constitution Principle V)
- [ ] T073 Run integration tests for TokenFactory endpoints
- [ ] T074 Generate master token for initial setup: `TokenFactory.GenerateMasterToken(TimeSpan.FromDays(30))`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
  - Constants classes must be created before user stories reference them
  - Configuration classes must exist before middleware/TokenFactory can use them
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User Story 1 (P1): X-Auth Middleware - can start after Foundational
  - User Story 2 (P1): TokenFactory - can start after Foundational
  - User Story 3 (P2): Constants - can start after Foundational
  - User Story 4 (P2): Subdomain Routing - can start after Foundational
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
  - X-Auth middleware can be tested independently with static tokens
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable
  - TokenFactory generates tokens that US1 middleware validates
  - Can test TokenFactory endpoints independently of middleware validation
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories
  - Constants are additive and replace hardcoded strings
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories
  - Deployment YAMLs are independent configuration

### Within Each User Story

- Models before services (for US2)
- Services before controllers (for US2)
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes:
  - US1 and US2 can be worked on in parallel (both P1)
  - US3 and US4 can be worked on in parallel (both P2)
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members
- All polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all model tasks for User Story 1 together:
Task: "Create TokenValidationResult.cs model in src/Common/Core/Authentication/"
Task: "Implement path-based scope mapping in XAuthTokenMiddleware.cs"

# Launch middleware implementation tasks (sequential due to file dependencies):
Task: "Update XAuthTokenMiddleware.cs header extraction logic"
Task: "Update XAuthTokenMiddleware.cs token validation logic with scope checking"
```

---

## Parallel Example: User Story 2

```bash
# Launch all model creation tasks together:
Task: "Create ScopedTokenData.cs internal model"
Task: "Create MasterTokenData.cs internal model"
Task: "Create ScopedTokenResult.cs public result model"

# Launch TokenFactory implementation tasks:
Task: "Update TokenFactory.cs master token validation"
Task: "Update TokenFactory.cs scoped token generation with HMAC-SHA256 signing"

# Launch controller endpoint tasks (after models complete):
Task: "Create TokenFactoryController.cs with POST /api/tokens/generate endpoint"
Task: "Create TokenFactoryController.cs with POST /api/tokens/generate-all endpoint"
Task: "Create TokenFactoryController.cs with POST /api/tokens/validate endpoint"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (X-Auth Middleware with static tokens)
4. **STOP and VALIDATE**: Test User Story 1 independently with static test tokens
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP with static tokens)
3. Add User Story 2 → Test independently → Deploy/Demo (zero-trust token generation)
4. Add User Story 3 → Test independently → Deploy/Demo (constants pattern)
5. Add User Story 4 → Test independently → Deploy/Demo (subdomain routing)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (X-Auth Middleware)
   - Developer B: User Story 2 (TokenFactory)
   - Developer C: User Story 3 (Constants)
   - Developer D: User Story 4 (Subdomain Routing)
3. Stories complete and integrate independently
4. US1 and US2 (P1) should complete before US3 and US4 (P2)

---

## Documentation Reconciliation

### New Documentation Required
- [x] Feature overview document (exists: spec.md)
- [x] Research findings (exists: research.md)
- [x] Data model documentation (exists: data-model.md)
- [x] Quickstart guide (exists: quickstart.md)
- [x] API contracts (exists: contracts/)

### Existing Documentation to Update
- [x] README.md - add authentication section
- [x] Deployment docs - add TokenFactory env vars

### Documentation Location
- **Specs**: `specs/002-xauth-token-factory/` (SpecKit documentation)
- **System docs**: Create `docs/` folder for non-SpecKit docs
- **Format**: Markdown with Mermaid diagrams

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
