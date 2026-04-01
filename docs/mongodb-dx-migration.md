# MongoDB DX Migration Guide

## Overview
This document outlines the migration from manual MongoDB configuration to DX infrastructure auto-generated environment variables.

## Changes Made

### 1. Environment Variables Constants
Created `src/Common/Core/Constants/EnvironmentVariables.cs` with standardized environment variable names:

```csharp
public static class EnvironmentVariables
{
    // DX Auto-generated MongoDB variables
    public const string MongoDbConnectionString = "MONGODB__CLUSTER__CONNECTION_STRING";
    public const string MongoDbServer = "MONGODB__CLUSTER__SERVER";
    public const string MongoDbTlsCertFile = "MONGODB__CLUSTER__TLS_CRT_KEY_FILE";
    
    // Legacy support
    public const string MongoDbDatabaseName = "MONGODB_DATABASE_NAME";
    
    // Other common variables
    public const string SendGridApiKey = "SENDGRID_API_KEY";
    public const string XauthMasterToken = "XAUTH_MASTER_TOKEN";
    // ... etc
}
```

### 2. MongoDB Configuration Updates
Updated `src/Common/Infrastructure/MongoDb/MongoDbConfiguration.cs`:

- **Before**: Used `configuration.GetSection("MongoDB")`
- **After**: Uses DX environment variables directly

```csharp
services.Configure<MongoDbSettings>(settings =>
{
    settings.ConnectionString = configuration[EnvironmentVariables.MongoDbConnectionString] ?? string.Empty;
    settings.Server = configuration[EnvironmentVariables.MongoDbServer] ?? string.Empty;
    settings.TlsCertFile = configuration[EnvironmentVariables.MongoDbTlsCertFile] ?? string.Empty;
    settings.DatabaseName = configuration[EnvironmentVariables.MongoDbDatabaseName] ?? string.Empty;
});
```

### 3. Deployment YAML Updates
Updated all API deployment files to use DX MongoDB infrastructure pattern:

#### Before (Manual Configuration)
```yaml
secretVars:
  MONGODB_CONNECTION_STRING: '#{Octopus.Variable["MongoDB.ConnectionString"]}'
  MONGODB_DATABASE_NAME: '#{Octopus.Variable["MongoDB.Database"]}'
```

#### After (DX Infrastructure)
```yaml
infrastructure:
  mongodb:
    atlas:
      user:
        cluster:
          project: io-common-api
          group: common-services
          env: dev
        roles:
          - name: readWrite
            database: io_common

secretVars:
  SENDGRID_API_KEY: '#{Octopus.Variable["SendGrid.ApiKey"]}'
  XAUTH_MASTER_TOKEN: '#{Octopus.Variable["XAuth.MasterToken"]}'
```

### 4. Updated APIs
All APIs now use DX MongoDB infrastructure:

| API | Database | Group |
|-----|----------|-------|
| io-common-api | io_common | common-services |
| io-cass-api | io_cass | carrier-vetting |
| io-elsa-api | io_elsa | pricing |
| io-larry-api | io_larry | vendor-management |
| io-lea-api | io_lea | job-processing |
| io-proxy-api | io_proxy | api-gateway |

## DX Auto-Generated Variables

When you add MongoDB infrastructure to your deployment YAML, DX automatically creates:

| Variable | Description | Example |
|----------|-------------|---------|
| `MONGODB__CLUSTER__CONNECTION_STRING` | Full connection string | `mongodb://user:pass@cluster.mongodb.net/db` |
| `MONGODB__CLUSTER__SERVER` | Server hostname | `cluster.mongodb.net` |
| `MONGODB__CLUSTER__TLS_CRT_KEY_FILE` | Certificate path | `/etc/mongodb/certs/tls-combined.pem` |

## Certificate Management

DX automatically:
- Generates MongoDB user certificates
- Mounts certificates at `/etc/mongodb/certs/tls-combined.pem`
- Creates secrets and configmaps with name `{name}-m-u`

## Benefits

1. **Simplified Configuration**: No manual MongoDB connection string management
2. **Automatic Certificate Handling**: DX manages TLS certificates automatically
3. **Consistent Pattern**: All APIs use the same environment variable pattern
4. **Better Security**: Certificates are managed by DX infrastructure
5. **Easier Onboarding**: New APIs just need to add the infrastructure section

## Migration Steps for New APIs

1. Add MongoDB infrastructure section to deployment YAML
2. Use `EnvironmentVariables` constants in code
3. Remove manual MongoDB variables from `secretVars`
4. Configure service-specific database name in `DatabaseName` property

## Backward Compatibility

The `MONGODB_DATABASE_NAME` variable is still supported for legacy configurations, but new APIs should use the DX infrastructure pattern.
