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

---

## .NET 10 Migration Strategy with CLEAN Architecture

### **Architecture Pattern: Following edi-platform**

Based on the edi-platform monorepo structure, we'll implement CLEAN architecture with:

```
src/
├── Common/
│   ├── Core/                    # Domain interfaces, entities, business logic
│   ├── Infrastructure/          # External integrations, data access
│   └── Models/                  # Standard type definitions
├── Apps/
│   ├── RestAPI/
│   │   ├── IO.Proxy/           # API Gateway
│   │   ├── IO.Common/          # Email + Context API
│   │   ├── IO.Cass/            # Carrier vetting API
│   │   └── IO.Elsa/            # Pricing API
│   ├── Handlers/               # Background processors
│   └── Jobs/                   # Scheduled tasks
└── Libraries/                  # Shared utilities
```

### **Technology Stack**

#### **Core Framework**
- **.NET 10** (latest LTS)
- **ASP.NET Core 10** for REST APIs
- **Worker Services** for background processing
- **Minimal APIs** for streamlined endpoints

#### **USXpress Standard Packages**
- `USXpress.Monitoring` v3.1.10+ for Grafana/OTEL
- `USXpress.Configuration.Mongo` v0.2.8+ for MongoDB
- `USXpress.Kafka` v0.0.5+ for messaging
- `USXpress.Api.Common` v0.1.13+ for shared patterns

#### **Infrastructure Integration**
- **MongoDB Atlas** for data persistence
- **Kafka** for event streaming
- **Azure AD** for authentication
- **OpenTelemetry** for observability

### **Deployment YAML Patterns (edi-platform style)**

#### **API Services Pattern**
```yaml
---
name: io-proxy-api
octopus:
  space: USXpress
  group: gateway
tags:
  owner: USXpress
  team: Platform
  purpose: API Gateway for IO services
infrastructure:
  auth:
    roles:
      - io-proxy-reader
      - io-proxy-writer
    group_roles_assignment:
      - name: Everybody
        roles:
          - io-proxy-reader
          - io-proxy-writer
    redirect_uri_paths:
      - path: /signin-oidc
        type: api
      - path: /swagger/oauth2-redirect.html
        type: spa
api:
  routes:
    subdomains:
      - product: io
        type: api
  global:
    upstream:
      perTryTimeout: 15s
  enabled: true
  service:
    targetPort: 8080
  configVars:
    APPLICATION_ENTRYPOINT: IO.Proxy.dll
    Serilog__MinimumLevel__Default: Information
  secretVars:
    AUTH__CLIENT_SECRET: '#{AUTH__CLIENT_SECRET}'
```

### **Common Libraries Structure**

#### **Core Library (`src/Common/Core/Core.csproj`)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="USXpress.Configuration.Mongo" />
    <PackageReference Include="USXpress.Monitoring" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../Models/IO.Standard.Types/IO.Standard.Types.csproj" />
  </ItemGroup>
</Project>
```

#### **Models Library (`src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj`)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="USXpress.Standard.Types" />
    <PackageReference Include="USXpress.Standard.Types.Common" />
  </ItemGroup>
</Project>
```

### **Dockerfile Multi-Stage Build (edi-platform pattern)**

```dockerfile
# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS restore
ARG GITHUB_TOKEN
ARG GITHUB_USER

WORKDIR /app
COPY nuget.config .
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["io-platform.sln", "."]

# Copy ALL project files for restore
COPY ["src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj", "src/Common/Models/IO.Standard.Types/"]
COPY ["src/Common/Core/Core.csproj", "src/Common/Core/"]
COPY ["src/Common/Infrastructure/Infrastructure.csproj", "src/Common/Infrastructure/"]
COPY ["src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj", "src/Apps/RestAPI/IO.Proxy/"]
# ... additional projects

# Restore all dependencies
ENV NUGET_XMLDOC_MODE=none
RUN dotnet restore io-platform.sln /p:WarningLevel=0

# Stage 2: Build Common/Shared projects
FROM restore AS build-common
COPY src/Common/ src/Common/
RUN dotnet build "src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj" -c Release --no-restore

# Stage 3: Build and Publish Apps
FROM build-common AS publish
COPY src/Apps/ src/Apps/
RUN dotnet publish "src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj" -c Release -o /app/publish --no-restore

# Stage 4: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IO.Proxy.dll"]
```

### **GitHub Actions Workflow (edi-platform pattern)**

```yaml
---
name: Build & Deploy

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  MASTER_BRANCH: main

on:
  push:
    paths:
      - '.octopus/deploy/**'
      - '.github/workflows/build.yaml'
      - 'src/**'

jobs:
  build:
    name: Build and Deploy
    runs-on: ubuntu-latest
    
    permissions:
      id-token: write
      contents: write
      
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
          
      - name: Artifact 📦
        uses: variant-inc/actions-dotnet@v2
        with:
          dotnet-version: 10.0.x
          ecr_repository: usxpress/io-platform
          
      - name: Release 🛸
        uses: variant-inc/actions-octopus@v3
        with:
          deploy_yaml_dir: .octopus/deploy
```

### **Monitoring Configuration (USXpress.Monitoring)**

#### **Program.cs Pattern**
```csharp
using USXpress.Monitoring;
using USXpress.Monitoring.Models;

var builder = WebApplication.CreateBuilder(args);

// Application metadata from configuration
var configuration = builder.Configuration;
var environment = Enum.Parse<MonitoringEnvironment>(
    configuration.GetValue<string>("APPLICATION:ENVIRONMENT") ?? "development",
    ignoreCase: true);
var project = configuration.GetValue<string>("APPLICATION:PROJECT") ?? "io-platform";
var group = configuration.GetValue<string>("APPLICATION:GROUP") ?? "gateway";

// Configure Serilog with console output
var loggingConfiguration = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

// Add monitoring (Grafana/OTEL)
builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
    EnableOtel = true,
    SerilogLoggerConfiguration = loggingConfiguration,
});
```

### **MongoDB Integration (USXpress.Configuration.Mongo)**

#### **Constants Pattern**
```csharp
namespace IO.Common.Constants;

public class MongoDbCollections
{
    public const string VendorCollection = $"{Prefix}:VendorCollection";
    public const string ContextCollection = $"{Prefix}:ContextCollection";
    private const string Prefix = "Database";
}

public class MongoDbDatabases
{
    public const string MainDatabaseName = $"{Prefix}:MainDatabaseName";
    private const string Prefix = "Database";
}

public class MongoDbClusterOptions
{
    public const string ConnectionString = $"{Prefix}:CONNECTION_STRING";
    public const string TlsCrtKeyFile = $"{Prefix}:TLS_CRT_KEY_FILE";
    private const string Prefix = "MONGODB:CLUSTER";
}
```

#### **Repository Extension**
```csharp
public static class MongoExtensions
{
    public static IHostApplicationBuilder AddMongoRepositories(
        this IHostApplicationBuilder builder)
    {
        builder.UseDefaultMongoConventions();
        return builder.AddVendorRepository().AddContextRepository();
    }

    public static IHostApplicationBuilder AddVendorRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[MongoDbClusterOptions.ConnectionString]!,
            DatabaseName = builder.Configuration[MongoDbDatabases.MainDatabaseName]!,
            CollectionName = builder.Configuration[MongoDbCollections.VendorCollection]!,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[MongoDbClusterOptions.TlsCrtKeyFile]
        };

        builder.Services.AddMongoRepository<Vendor>(config);
        return builder;
    }
}
```

### **Migration Phases**

#### **Phase 1: Foundation (Week 1-2)**
1. Create monorepo structure with .NET 10
2. Set up Common libraries (Core, Infrastructure, Models)
3. Configure GitHub Actions and Dockerfile
4. Set up USXpress.Monitoring and USXpress.Configuration.Mongo
5. Create deployment YAML templates

#### **Phase 2: Proxy API (Week 3)**
1. Implement IO.Proxy API gateway
2. Configure routing and authentication
3. Set up service discovery patterns
4. Deploy and test proxy functionality

#### **Phase 3: Domain APIs (Week 4-8)**
1. **IO.Common**: Migrate email and context services
2. **IO.Cass**: Migrate carrier vetting with Highway/Mcleod APIs
3. **IO.Elsa**: Migrate pricing calculations

#### **Phase 4: Background Processing (Week 9-10)**
1. Convert scheduled jobs to .NET Worker Services
2. Implement Kafka consumers/producers
3. Set up MongoDB repositories for each domain
4. Configure monitoring and logging

#### **Phase 5: Testing & Migration (Week 11-12)**
1. Comprehensive integration testing
2. Performance testing and optimization
3. Gradual traffic migration
4. Decommission Python monolith

### **Benefits of .NET 10 Migration**

1. **Performance**: .NET 10 offers significant performance improvements
2. **Ecosystem**: Full access to USXpress NuGet packages and tooling
3. **Observability**: Native integration with USXpress monitoring stack
4. **Maintainability**: Strong typing and compile-time safety
5. **Scalability**: Better containerization and orchestration support
6. **Security**: Built-in security features and Azure AD integration
7. **Developer Experience**: Superior IDE support and debugging capabilities
