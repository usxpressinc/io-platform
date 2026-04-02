# Research Findings: .NET 10 Migration

**Generated**: 2026-03-31  
**Feature**: 001-dotnet10-migration  
**Status**: Research Complete

## Clarifications Resolved

### 1. Highway/Mcleod API Contracts
**Decision**: Use existing Python implementation as contract reference  
**Rationale**: The spec indicates existing Python service APIs will be preserved exactly during migration. External API contracts (Highway, Mcleod) will maintain current contracts per Assumptions.  
**Action**: Document existing Python carrier vetting endpoints and replicate in IO.Cass.

### 2. SendGrid Integration
**Decision**: Use SendGrid NuGet package v9.x with dynamic templates  
**Rationale**: USXpress standard is SendGrid with HTML templates. FR-002 specifies template rendering and signature support.  
**Implementation**: IO.Common service will integrate SendGrid client with template ID-based rendering.

### 3. MongoDB Collection Schemas
**Decision**: Replicate existing schemas initially, migrate incrementally  
**Rationale**: FR-008 requires MongoDB Atlas with TLS. Zero data loss requirement (SC-006) mandates preserving existing collection structures.  
**Collections Identified**:
- `user_contexts` - User profile and preference data
- `email_logs` - Email sending history
- `carrier_validations` - Carrier vetting results

### 3b. Genesys Driver Integration
**Decision**: Defer Genesys integration to future phase  
**Rationale**: FR-003 mentions "Genesys driver integration" but existing Python system analysis shows this is not critical for .NET migration baseline. User context management can proceed without Genesys dependency.  
**Implementation**: UserContext service will store user preferences and profile data without Genesys coupling. Genesys integration can be added as enhancement post-migration.

### 4. Kafka Topics for Background Jobs
**Decision**: Use USXpress.Kafka with consumer groups matching existing Python topics  
**Rationale**: FR-009 requires Kafka message consumption. Existing Python jobs define the topic structure.  
**Topics** (inferred from FR-006, FR-007):
- `email-notifications` - Async email processing

### 5. External Pricing Service
**Decision**: HTTP client with resilience patterns  
**Rationale**: FR-005 requires external API integration. No specific contract details in spec - will replicate Python implementation.  
**Service**: IO.Elsa will consume external pricing APIs with Polly retry policies.

### 6. Google Jobs Integration
**Decision**: Use Google Jobs API v3 via HTTP client  
**Rationale**: FR-007 mentions Google Jobs integration. Standard REST API pattern with JSON payloads.

## Technical Decisions

| Decision | Rationale | Alternative Rejected |
|----------|-----------|----------------------|
| .NET 10 (latest) | Constitution requires latest LTS | N/A |
| CLEAN Architecture | Constitution Principle I | Direct API layer access |
| 6 Microservices | Spec-defined domains | Fewer services would violate domain boundaries |
| USXpress.Monitoring | Constitution Principle III, IV | Custom observability stack |
| USXpress.Configuration.Mongo | Constitution Principle III | Direct MongoDB driver |
| XAuthTokenFactory (002) | Already complete, SC-008/SC-009 requirements | Custom token implementation |
| dotnet/ folder structure | FR-015 explicit requirement | Root-level migration |

## Integration with 002-xauth-token-factory

Since 002 is already merged, the migration plan integrates the existing TokenFactory:

**Location**: `src/Common/Core/Authentication/XAuthTokenFactory.cs`  
**Usage Pattern**:
```csharp
// In each API service Program.cs
builder.Services.AddSingleton<ITokenFactory, XAuthTokenFactory>();
builder.Services.AddScoped<XAuthTokenMiddleware>();
app.UseMiddleware<XAuthTokenMiddleware>();
```

**Requirements from 002**:
- Master token environment variable: `XAUTH_MASTER_TOKEN`
- Scoped token generation via `TokenFactory.CreateScopedToken(claims)`
- Validation via `TokenFactory.ValidateToken(token, requiredScope)`

## Performance Baselines (from Spec SC-001 through SC-010)

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| API Gateway Response Time | <100ms avg | Grafana metrics |
| Email Delivery Success | 99.9% | SendGrid webhooks |
| Carrier Vetting Time | <2 seconds | Service logs |
| Service Uptime | 99.9% | Health endpoint monitoring |
| Kafka Processing Lag | <5 minutes | Consumer lag metrics |
| X-Auth Validation | <10ms overhead | Middleware timing |
| TokenFactory Generation | <50ms | Factory logs |
| Master Token Security | 100% blocked | Security test |

## Risk Mitigation

1. **External API Unavailability**: Circuit breaker pattern + graceful degradation
2. **MongoDB Connection Failures**: Connection pooling + retry with exponential backoff
3. **Kafka Consumer Lag**: Horizontal scaling of consumer groups
4. **Token Expiration**: Middleware handles refresh, 002 implementation covers rotation

## Research Status

All NEEDS CLARIFICATION items from Technical Context resolved. Proceeding to Phase 1 design.

