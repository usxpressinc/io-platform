# Data Model: IO Platform .NET Migration

**Feature**: 001-dotnet10-migration  
**Generated**: 2026-03-31

## Entity Overview

Per spec requirements FR-001 through FR-016, the following entities are defined across domain microservices.

---

## IO.Common Domain

### EmailRequest
**Collection**: `email_logs`  
**Purpose**: Email sending request with recipients, content, and formatting

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string (ObjectId) | Yes | Unique identifier |
| ToAddresses | string[] | Yes | Recipient email addresses |
| CcAddresses | string[] | No | CC recipients |
| BccAddresses | string[] | No | BCC recipients |
| FromAddress | string | Yes | Sender email |
| Subject | string | Yes | Email subject line |
| Body | string | Yes | Email body (HTML or text) |
| IsHtml | bool | Yes | Body format flag |
| TemplateId | string | No | SendGrid template ID |
| TemplateData | Dictionary<string,object> | No | Template substitution data |
| Attachments | Attachment[] | No | File attachments |
| Status | EmailStatus | Yes | Pending, Sent, Failed |
| SentAt | DateTime? | No | Timestamp when sent |
| ErrorMessage | string | No | Failure reason if Failed |
| CreatedAt | DateTime | Yes | Request creation timestamp |
| CorrelationId | string | Yes | Tracing correlation ID |

**Validation Rules**:
- At least one ToAddress required
- FromAddress must be valid email format
- If TemplateId provided, TemplateData required

---

### UserContext
**Collection**: `user_contexts`  
**Purpose**: User profile and preference data for personalization

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string (ObjectId) | Yes | Unique identifier |
| UserId | string | Yes | External user identifier (Genesys) |
| Email | string | Yes | User email address |
| DisplayName | string | Yes | User's display name |
| Preferences | UserPreferences | Yes | User preference settings |
| Roles | string[] | Yes | User role assignments |
| LastLoginAt | DateTime | No | Last login timestamp |
| CreatedAt | DateTime | Yes | Account creation timestamp |
| UpdatedAt | DateTime | Yes | Last update timestamp |

**Nested: UserPreferences**

| Field | Type | Description |
|-------|------|-------------|
| TimeZone | string | User's preferred timezone |
| Locale | string | Language/locale code |
| NotificationSettings | Dictionary | Email/SMS preferences |
| DashboardLayout | object | UI customization data |

---

## IO.Cass Domain

### CarrierValidation
**Collection**: `carrier_validations`  
**Purpose**: Carrier eligibility assessment with contacts and compliance status

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string (ObjectId) | Yes | Unique identifier |
| DotNumber | string | Yes | DOT registration number |
| McNumber | string | No | MC number if applicable |
| CarrierName | string | Yes | Carrier company name |
| Status | ValidationStatus | Yes | Valid, Invalid, Pending, Error |
| HighwayData | HighwayResponse | No | Highway API response data |
| McleodData | McleodResponse | No | Mcleod API response data |
| Contacts | CarrierContact[] | No | Carrier contact information |
| ValidationErrors | string[] | No | List of validation failures |
| ValidatedAt | DateTime? | No | When validation completed |
| ExpiresAt | DateTime | Yes | Validation expiration |
| CreatedAt | DateTime | Yes | Validation request timestamp |
| CorrelationId | string | Yes | Tracing correlation ID |

**Nested: CarrierContact**

| Field | Type | Description |
|-------|------|-------------|
| Type | string | Contact type (Dispatch, Safety, etc.) |
| Name | string | Contact person name |
| Phone | string | Phone number |
| Email | string | Email address |
| IsPrimary | bool | Primary contact flag |

**Validation Rules** (per FR-004):
- DotNumber or McNumber required
- Status transitions: Pending → Valid/Invalid/Error
- ExpiresAt must be > CreatedAt

---

## IO.Elsa Domain

### PricingCalculation
**Collection**: `pricing_calculations`  
**Purpose**: Load pricing breakdown with distance, base cost, and surcharges

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string (ObjectId) | Yes | Unique identifier |
| LoadId | string | Yes | Associated load identifier |
| Origin | Location | Yes | Pickup location |
| Destination | Location | Yes | Delivery location |
| Distance | decimal | Yes | Calculated distance in miles |
| BaseCost | decimal | Yes | Base transportation cost |
| FuelSurcharge | decimal | Yes | Fuel cost adjustment |
| Accessorials | AccessorialCharge[] | No | Additional service charges |
| TotalCost | decimal | Yes | Sum of all charges |
| Currency | string | Yes | ISO currency code (USD) |
| CalculatedAt | DateTime | Yes | Calculation timestamp |
| ExpiresAt | DateTime | Yes | Quote expiration |
| ExternalQuoteId | string | No | External pricing service reference |
| Status | PricingStatus | Yes | Valid, Expired, Error |
| CreatedAt | DateTime | Yes | Record creation timestamp |

**Nested: Location**

| Field | Type | Description |
|-------|------|-------------|
| City | string | City name |
| State | string | State code |
| ZipCode | string | Postal code |
| Country | string | Country code |
| Latitude | decimal? | GPS latitude |
| Longitude | decimal? | GPS longitude |

**Nested: AccessorialCharge**

| Field | Type | Description |
|-------|------|-------------|
| Type | string | Charge type (Liftgate, Inside, etc.) |
| Description | string | Human-readable description |
| Amount | decimal | Charge amount |

---

## Cross-Domain Entities

### ServiceHealth
**Purpose**: Microservice health status with dependencies and performance metrics
**Usage**: All services expose this via /health endpoint

| Field | Type | Description |
|-------|------|-------------|
| ServiceName | string | Service identifier |
| Status | HealthStatus | Healthy, Degraded, Unhealthy |
| Version | string | Service version |
| Uptime | TimeSpan | Service uptime |
| Dependencies | DependencyHealth[] | External dependency status |
| Performance | PerformanceMetrics | Current performance data |
| CheckedAt | DateTime | Health check timestamp |

### XAuthToken
**Purpose**: Authentication token with scope validation
**Usage**: All API services (via 002-xauth-token-factory)

| Field | Type | Description |
|-------|------|-------------|
| Token | string | JWT token string |
| Scope | string | Token scope/permission |
| ExpiresAt | DateTime | Token expiration |
| IssuedAt | DateTime | Token issuance |
| IsMaster | bool | Master token flag (cannot access APIs) |

---

## State Transitions

### EmailRequest Status
```
Pending → Sent
Pending → Failed
```

### CarrierValidation Status
```
Pending → Valid
Pending → Invalid
Pending → Error
```

### PricingCalculation Status
```
Valid → Expired
Pending → Valid
Pending → Error
```

### JobPosting Status
```
Active → Filled
Active → Expired
Draft → Active
```

## MongoDB Index Recommendations

| Collection | Index Fields | Type |
|------------|--------------|------|
| email_logs | Status, CreatedAt | Compound |
| email_logs | CorrelationId | Single |
| user_contexts | UserId | Unique |
| user_contexts | Email | Single |
| carrier_validations | DotNumber | Single |
| carrier_validations | Status, ValidatedAt | Compound |
| pricing_calculations | LoadId | Single |
| pricing_calculations | Status, ExpiresAt | Compound |

