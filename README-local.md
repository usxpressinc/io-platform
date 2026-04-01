# IO Platform - Local Development Setup

## Quick Start

### Prerequisites
- Docker Desktop (or Docker Engine)
- .NET 8 SDK
- Git

### 1. Start Infrastructure Services
```bash
# Start MongoDB, Kafka, Prometheus, Grafana
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to be ready (about 30-60 seconds)
docker-compose -f docker-compose.dev.yml ps
```

### 2. Run .NET Services Locally
```bash
# Build the solution
dotnet build src/io-platform.sln

# Run each service in separate terminals
# Terminal 1 - API Gateway
cd src/Apps/RestAPI/IO.Proxy
dotnet run --urls "http://localhost:8080"

# Terminal 2 - Common Services
cd src/Apps/RestAPI/IO.Common
dotnet run --urls "http://localhost:8081"

# Terminal 3 - Carrier Vetting
cd src/Apps/RestAPI/IO.Cass
dotnet run --urls "http://localhost:8082"

# Terminal 4 - Pricing Service
cd src/Apps/RestAPI/IO.Elsa
dotnet run --urls "http://localhost:8083"

# Terminal 5 - Vendor Management
cd src/Apps/RestAPI/IO.Larry
dotnet run --urls "http://localhost:8084"

# Terminal 6 - Job Processing
cd src/Apps/RestAPI/IO.Lea
dotnet run --urls "http://localhost:8085"
```

### 3. Access Services

#### API Endpoints
- **API Gateway**: http://localhost:8080
- **Common Services**: http://localhost:8081
- **Carrier Vetting**: http://localhost:8082
- **Pricing Service**: http://localhost:8083
- **Vendor Management**: http://localhost:8084
- **Job Processing**: http://localhost:8085

#### Monitoring & Tools
- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **MongoDB**: localhost:27017 (admin/password123)
- **Kafka**: localhost:9092

#### Health Checks
All services have health endpoints:
- http://localhost:8080/health
- http://localhost:8081/health
- http://localhost:8082/health
- http://localhost:8083/health
- http://localhost:8084/health
- http://localhost:8085/health

#### API Documentation (Swagger)
- http://localhost:8080/swagger
- http://localhost:8081/swagger
- http://localhost:8082/swagger
- http://localhost:8083/swagger
- http://localhost:8084/swagger
- http://localhost:8085/swagger

### 4. Test the Services

#### Test API Gateway
```bash
curl -H "Authorization: Bearer demo-token-12345" http://localhost:8080/health
```

#### Test Common Services
```bash
# Get user context
curl -H "Authorization: Bearer demo-token-12345" \
     http://localhost:8081/api/usercontext/user-123

# Send email
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"toEmail":"test@example.com","subject":"Test","body":"Hello World"}' \
     http://localhost:8081/api/email/send
```

#### Test Carrier Vetting
```bash
# Vet a carrier
curl -X POST -H "Authorization: Bearer demo-token-12345" \
     http://localhost:8082/api/carrier/123456789/vet
```

#### Test Pricing Service
```bash
# Calculate price
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"customerId":"customer-123","serviceType":"transportation","distance":100,"weight":1000,"volume":10}' \
     http://localhost:8083/api/pricing/calculate
```

#### Test Vendor Management
```bash
# Create vendor
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"vendorCode":"TEST-001","name":"Test Vendor","contactEmail":"test@vendor.com","services":["transportation"]}' \
     http://localhost:8084/api/vendor
```

#### Test Job Processing
```bash
# Create job
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"customerId":"customer-123","title":"Test Job","description":"Test Description","location":"Test City","employmentType":"Full-time","companyName":"Test Company","applicationUrl":"http://example.com"}' \
     http://localhost:8085/api/job
```

## Environment Variables

The services use these environment variables (configured in docker-compose.dev.yml):

```bash
# Common
ASPNETCORE_ENVIRONMENT=Development
ApplicationEnvironment=development
ApplicationProject=io-platform
KAFKA_BOOTSTRAP_SERVERS=kafka:9092
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317

# MongoDB
MONGODB_CONNECTION_STRING=mongodb://admin:password123@mongodb:27017/io-platform?authSource=admin
MONGODB_DATABASE_NAME=io-platform

# Authentication
XAUTH_MASTER_TOKEN=demo-token-12345

# Service-specific
SENDGRID_API_KEY=SG.demo-key-12345
HIGHWAY_API_KEY=highway-demo-key-12345
MCLEOD_API_KEY=mcleod-demo-key-12345
GOOGLE_JOBS_API_KEY=google-jobs-demo-key-12345
```

## Development Tips

### 1. Hot Reload
Use `dotnet watch run` for hot reload during development:
```bash
cd src/Apps/RestAPI/IO.Proxy
dotnet watch run --urls "http://localhost:8080"
```

### 2. Debugging
Set breakpoints in Visual Studio or VS Code and attach to the running process.

### 3. Database Access
Connect to MongoDB using:
```bash
mongosh mongodb://admin:password123@localhost:27017/io-platform?authSource=admin
```

### 4. Kafka Topics
View Kafka topics:
```bash
docker exec io-kafka kafka-topics --bootstrap-server localhost:9092 --list
```

### 5. Logs
View logs for any service:
```bash
# For Docker services
docker-compose -f docker-compose.dev.yml logs -f mongodb

# For .NET services (running locally)
# Check console output or use dotnet run with verbosity
dotnet run --urls "http://localhost:8080" --verbosity normal
```

## Cleanup

Stop all services:
```bash
# Stop infrastructure
docker-compose -f docker-compose.dev.yml down

# Stop .NET services (Ctrl+C in each terminal)
```

Remove all data (optional):
```bash
docker-compose -f docker-compose.dev.yml down -v
docker system prune -f
```

## Troubleshooting

### Port Conflicts
If ports are in use, modify the ports in docker-compose.dev.yml or use different ports for .NET services.

### MongoDB Connection Issues
Ensure MongoDB is fully started before running .NET services. Check health:
```bash
docker exec io-mongodb mongosh --eval "db.adminCommand('ping')"
```

### Kafka Connection Issues
Check Kafka health:
```bash
docker exec io-kafka kafka-broker-api-versions --bootstrap-server localhost:9092
```

### Service Startup Issues
Check .NET service logs for detailed error information. Ensure all dependencies (MongoDB, Kafka) are running.

### Authentication Issues
Use the demo token: `demo-token-12345` for all API calls during local development.

## Full Docker Mode (Optional)

If you prefer to run everything in Docker (including .NET services):

```bash
# Build and run all services
docker-compose up -d

# This will build the .NET services and run them in containers
# Note: This requires the Dockerfile to be properly configured
```

Access endpoints remain the same, but all services run in containers.
