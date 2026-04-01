# Docker Compose Environment Setup

## Overview
Updated Docker Compose configuration to use individual .env files for each API service, following USXpress MongoDB configuration patterns.

## Changes Made

### 1. MongoDB Configuration Fix
**Before**: Custom MongoDB configuration with made-up patterns
**After**: Proper USXpress.Configuration.Mongo NuGet package usage

```csharp
// Updated Program.cs files
builder.AddMongoDb()
       .AddEmailLogsRepository()  // For IO.Common
       .AddContextDataRepository(); // For IO.Common
```

### 2. Environment Variables Pattern
Using DX MongoDB environment variable pattern:

```bash
# DX Auto-generated Variables
MONGODB__CLUSTER__CONNECTION_STRING=mongodb://admin:password123@mongodb:27017/io_common?authSource=admin
MONGODB__CLUSTER__SERVER=mongodb
MONGODB__CLUSTER__TLS_CRT_KEY_FILE=
MONGODB_DATABASE_NAME=io_common

# Collection Configuration
DATABASE__EMAIL_LOGS_COLLECTION=email_logs
DATABASE__CONTEXT_DATA_COLLECTION=context_data
DATABASE__MAX_CONNECTION_POOL_SIZE=100
```

### 3. Individual .env Files
Created separate .env files for each service:

| Service | .env File | Database | Collections |
|---------|-----------|----------|-------------|
| IO.Proxy | `.env.io-proxy` | io_proxy | token_audits |
| IO.Common | `.env.io-common` | io_common | email_logs, context_data |
| IO.Cass | `.env.io-cass` | io_cass | carrier_data |
| IO.Elsa | `.env.io-elsa` | io_elsa | pricing_data |
| IO.Larry | `.env.io-larry` | io_larry | vendor_data |
| IO.Lea | `.env.io-lea` | io_lea | job_listings |

### 4. Docker Compose Updates
Updated `docker-compose.yml` to use `env_file` instead of inline environment variables:

```yaml
io-common:
  build:
    context: .
    dockerfile: Dockerfile
    target: base
  container_name: io-common
  restart: unless-stopped
  ports:
    - "8081:8081"
  env_file:
    - .env.io-common  # <-- Changed from inline environment
  depends_on:
    - mongodb
    - kafka
    - otel-collector
  networks:
    - io-network
```

## USXpress MongoDB Configuration

### NuGet Package
```bash
dotnet add package USXpress.Configuration.Mongo
```

### Repository Pattern
```csharp
// In MongoDbConfiguration.cs
public static IHostApplicationBuilder AddEmailLogsRepository(
    this IHostApplicationBuilder builder)
{
    var config = new MongoDbConfig
    {
        ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
        DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
        CollectionName = MongoDbCollections.EmailLogs,
        MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
        TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
    };

    builder.Services.AddMongoRepository<EmailLog>(config);
    return builder;
}
```

### Environment Variables Constants
```csharp
// In EnvironmentVariables.cs
public const string MongoDbConnectionString = "MONGODB__CLUSTER__CONNECTION_STRING";
public const string MongoDbServer = "MONGODB__CLUSTER__SERVER";
public const string MongoDbTlsCertFile = "MONGODB__CLUSTER__TLS_CRT_KEY_FILE";
public const string MongoDbDatabaseName = "MONGODB_DATABASE_NAME";
```

## Usage

### Start All Services
```bash
docker-compose up -d
```

### Start Specific Service
```bash
docker-compose up -d io-common
```

### View Service Logs
```bash
docker-compose logs -f io-common
```

### Update Environment Variables
Edit the corresponding `.env.{service}` file and restart:
```bash
docker-compose restart io-common
```

## Benefits

1. **Proper USXpress Patterns**: Uses official USXpress.Configuration.Mongo package
2. **Clean Configuration**: Each service has its own .env file
3. **DX Compatibility**: Follows DX infrastructure patterns
4. **Easy Local Development**: Simple to modify local environment variables
5. **Production Ready**: Same patterns work in DX deployment

## Environment Variable Reference

### Common Variables (All Services)
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Service binding URL
- `APPLICATION__ENVIRONMENT`: App environment
- `APPLICATION__PROJECT`: Project name
- `APPLICATION__GROUP`: Service group
- `KAFKA_BOOTSTRAP_SERVERS`: Kafka connection
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry endpoint
- `XAUTH_MASTER_TOKEN`: Authentication token

### MongoDB Variables (DX Pattern)
- `MONGODB__CLUSTER__CONNECTION_STRING`: Full connection string
- `MONGODB__CLUSTER__SERVER`: MongoDB server
- `MONGODB__CLUSTER__TLS_CRT_KEY_FILE`: TLS certificate path
- `MONGODB_DATABASE_NAME`: Database name
- `DATABASE__{COLLECTION}_COLLECTION`: Collection name
- `DATABASE__MAX_CONNECTION_POOL_SIZE`: Connection pool size

### Service-Specific Variables
- `SENDGRID_API_KEY`: Email service (IO.Common)
- `HIGHWAY_API_KEY`: Highway API (IO.Cass)
- `MCLEOD_API_KEY`: McLeod API (IO.Cass)
- `GOOGLE_JOBS_API_KEY`: Google Jobs API (IO.Lea)
