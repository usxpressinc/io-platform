# Domain Breakdown Plan for API Separation

This plan breaks down the current monolithic FastAPI application into logical domains that can be separated into individual microservices with a proxy API for routing.

## Current Domain Analysis

### **1. IO.COMMON Domain**
**Purpose**: Shared utilities and common services
**Endpoints**:
- `POST /api/common/email` - SendGrid email service
- `GET/POST /api/common/context` - User context management  
- `GET /api/common/genesys/driver` - Genesys driver context

**Dependencies**:
- SendGrid API (`Email_SendgridKey`, `Email_SignatureLogoUrl`)
- MongoDB for context storage (`MongoDbConnectionString`, `MongoDbTlsFile`)
- External driver service (`Get_Driver_Host`, `Get_Driver_Auth`)
- Jinja2 templates for email signatures
- Authentication scope: `["common"]`

**External Integrations**:
- SendGrid for email delivery
- MongoDB Atlas for user context
- External driver lookup service

**Background Processing**: None

---

### **2. IO.CASS Domain** 
**Purpose**: Carrier vetting and validation (CLARA system)
**Endpoints**:
- `POST /api/clara/carriers/valid` - Highway API carrier validation

**Dependencies**:
- Highway API (`Clara_HighwayUrl`, `Clara_HighwayApiKey`)
- Mcleod API (`Clara_McleodUrl`, `Clara_McleodAuth`, `Clara_McleodCompany`)
- Complex business logic for carrier qualification rules
- Authentication scope: `["clara"]`

**External Integrations**:
- Highway API for carrier data and rules assessment
- Mcleod API for carrier qualification checks
- Complex rule engine for compliance validation

**Background Processing**: None

**Key Features**:
- Multi-step carrier validation (Highway + Mcleod)
- Complex rule classification system
- Contact information aggregation
- Error classification and handling

---

### **3. IO.ELSA Domain**
**Purpose**: Pricing and load cost calculations
**Endpoints**:
- `POST /api/elsa/price/lookup` - Load pricing calculations

**Dependencies**:
- Pricing API (`Elsa_pricing_api`)
- On-prem proxy (`OnPrem_proxy`)
- Authentication scope: `["elsa"]`

**External Integrations**:
- External pricing service (SPAPI)
- Proxy for on-premise connectivity

**Background Processing**: None

**Key Features**:
- Stop sequence processing
- Price extraction from complex response structures
- Distance and cost calculations

---

### **4. IO.LARRY Domain**
**Purpose**: Vendor lookup and location-based services
**Endpoints**:
- `POST /api/larry/vendor/lookup` - Vendor location lookup

**Dependencies**:
- XPM API (`Larry_xpm_api`)
- Authentication scope: `["larry"]`

**External Integrations**:
- XPM vendor management system

**Background Processing**: 
- Scheduled vendor code updates (30-minute intervals)

**Key Features**:
- Location-based vendor searches
- Scheduled data synchronization

---

### **5. IO.LEA Domain**
**Purpose**: Job posting and geographic search
**Endpoints**:
- `POST /api/lea/jobs/lookup` - Geographic job search

**Dependencies**:
- Google Maps KML data (`Nora_GoogleMapsKml`)
- GeoServices for location matching
- Authentication scope: `["lea"]`

**External Integrations**:
- Geographic location services
- Polygon-based job matching

**Background Processing**:
- Scheduled Google Jobs updates (30-minute intervals)

**Key Features**:
- Geographic polygon matching
- Distance-based job filtering
- Point-in-polygon calculations

---

## Shared Infrastructure Components

### **IO.PROXY Domain (New)**
**Purpose**: External API gateway and request routing
**Responsibilities**:
- Request routing to appropriate domain APIs
- Authentication and authorization
- Rate limiting and request validation
- CORS handling
- API documentation aggregation

**Dependencies**:
- Authentication system (`Auth_ClientId`, `Auth_ClientSecret`, `Auth_TenantId`)
- Service discovery configuration
- Routing rules and domain mappings

---

### **Shared Services**

#### **Authentication**
- MSAL/Azure AD integration
- Scope-based authorization
- Token validation

#### **Monitoring**
- OpenTelemetry integration
- Centralized logging
- Metrics collection

#### **Background Processing**
- Kafka consumer for event processing
- Scheduled job management
- Service lifecycle management

#### **Database**
- MongoDB Atlas integration
- TLS certificate management
- Connection pooling

## Separation Strategy

### **Phase 1: Extract Shared Infrastructure**
1. Create `io-proxy` service with routing logic
2. Extract authentication into shared library
3. Standardize monitoring and logging

### **Phase 2: Domain Separation**
1. **io-common**: Email + Context services
2. **io-cass**: Carrier vetting logic
3. **io-elsa**: Pricing calculations
4. **io-larry**: Vendor lookup + scheduled updates
5. **io-lea**: Job search + scheduled updates

### **Phase 3: Migration**
1. Deploy domain APIs independently
2. Update proxy routing configuration
3. Migrate client traffic gradually
4. Decommission monolithic application

## Inter-Domain Communication
- Direct HTTP calls between domains (minimal)
- Shared event bus for async communication
- Common data models library
- Centralized configuration management

## Deployment Considerations
- Independent scaling per domain
- Separate CI/CD pipelines
- Domain-specific resource allocation
- Isolated failure domains
