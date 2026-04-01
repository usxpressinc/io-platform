# 🚀 IO Platform - Local Development Guide

## ✅ Infrastructure Status: RUNNING

All infrastructure services are up and ready:
- MongoDB: `localhost:27017` (admin/password123)
- Kafka: `localhost:9092`
- Prometheus: `localhost:9090`
- Grafana: `localhost:3000` (admin/admin123)
- OpenTelemetry: `localhost:4317/4318`

## 🛠️ Package Conflict Resolution

The current build has package conflicts due to central package management. Here are 3 ways to run the services:

### Option 1: Quick Docker Run (Recommended)
```bash
# Run all services in Docker (no build issues)
docker-compose up -d

# Wait 2-3 minutes for services to start
docker-compose ps

# Test endpoints
curl http://localhost:8080/health
curl http://localhost:8081/health
curl http://localhost:8082/health
curl http://localhost:8083/health
curl http://localhost:8084/health
curl http://localhost:8085/health
```

### Option 2: Fix Package Issues (For Local Development)
```bash
# Step 1: Remove conflicting Directory.Packages.props
mv Directory.Packages.props Directory.Packages.props.bak

# Step 2: Clear NuGet cache
dotnet nuget locals all --clear

# Step 3: Restore packages
dotnet restore io-platform.sln

# Step 4: Build and run individual services
cd src/Apps/RestAPI/IO.Proxy
dotnet run --urls "http://localhost:8080"

# In separate terminals:
cd src/Apps/RestAPI/IO.Common
dotnet run --urls "http://localhost:8081"

cd src/Apps/RestAPI/IO.Cass
dotnet run --urls "http://localhost:8082"

cd src/Apps/RestAPI/IO.Elsa
dotnet run --urls "http://localhost:8083"

cd src/Apps/RestAPI/IO.Larry
dotnet run --urls "http://localhost:8084"

cd src/Apps/RestAPI/IO.Lea
dotnet run --urls "http://localhost:8085"
```

### Option 3: Use Pre-built Docker Images
```bash
# Pull and run pre-built images
docker run -d --name io-proxy -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e MONGODB_CONNECTION_STRING=mongodb://admin:password123@host.docker.internal:27017/io-platform?authSource=admin \
  usxpress/io-proxy:latest

docker run -d --name io-common -p 8081:8081 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e MONGODB_CONNECTION_STRING=mongodb://admin:password123@host.docker.internal:27017/io-platform?authSource=admin \
  usxpress/io-common:latest

# ... continue for other services
```

## 🧪 Testing the Services

### Health Checks
```bash
# Test all services
curl http://localhost:8080/health
curl http://localhost:8081/health
curl http://localhost:8082/health
curl http://localhost:8083/health
curl http://localhost:8084/health
curl http://localhost:8085/health
```

### API Examples
```bash
# API Gateway
curl -H "Authorization: Bearer demo-token-12345" http://localhost:8080/health

# Common Services - Send Email
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"toEmail":"test@example.com","subject":"Test","body":"Hello World"}' \
     http://localhost:8081/api/email/send

# Carrier Vetting
curl -X POST -H "Authorization: Bearer demo-token-12345" \
     http://localhost:8082/api/carrier/123456789/vet

# Pricing Service
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"customerId":"customer-123","serviceType":"transportation","distance":100,"weight":1000,"volume":10}' \
     http://localhost:8083/api/pricing/calculate

# Vendor Management
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"vendorCode":"TEST-001","name":"Test Vendor","contactEmail":"test@vendor.com","services":["transportation"]}' \
     http://localhost:8084/api/vendor

# Job Processing
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer demo-token-12345" \
     -d '{"customerId":"customer-123","title":"Test Job","description":"Test Description","location":"Test City","employmentType":"Full-time","companyName":"Test Company","applicationUrl":"http://example.com"}' \
     http://localhost:8085/api/job
```

### Swagger Documentation
- API Gateway: http://localhost:8080/swagger
- Common Services: http://localhost:8081/swagger
- Carrier Vetting: http://localhost:8082/swagger
- Pricing Service: http://localhost:8083/swagger
- Vendor Management: http://localhost:8084/swagger
- Job Processing: http://localhost:8085/swagger

## 📊 Monitoring

### Grafana Dashboards
- URL: http://localhost:3000
- Login: admin/admin123
- Go to Dashboards → IO Platform

### Prometheus Metrics
- URL: http://localhost:9090
- Targets: http://localhost:9090/targets

### MongoDB Access
```bash
# Connect to MongoDB
mongosh mongodb://admin:password123@localhost:27017/io-platform?authSource=admin

# View collections
show collections
```

## 🔍 Troubleshooting

### Port Conflicts
If ports are in use:
```bash
# Check what's using ports
netstat -ano | findstr :8080

# Kill processes if needed
taskkill /PID <PID> /F
```

### Docker Issues
```bash
# Restart Docker services
docker-compose -f docker-compose.dev.yml restart

# View logs
docker-compose -f docker-compose.dev.yml logs -f

# Clean up
docker-compose -f docker-compose.dev.yml down -v
```

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## 🎯 Success Criteria

When everything is working, you should see:
- ✅ All 6 services responding to health checks
- ✅ Swagger UI accessible for all services
- ✅ Grafana showing metrics
- ✅ MongoDB with sample data
- ✅ API calls returning responses

## 🚀 Next Steps

1. **Choose your preferred option** (Docker recommended for quick start)
2. **Run the services** using the commands above
3. **Test the APIs** using the provided examples
4. **Monitor** via Grafana and Prometheus
5. **Develop** by modifying the code and restarting services

## 📞 Support

If you encounter issues:
1. Check Docker is running: `docker version`
2. Verify infrastructure: `docker-compose ps`
3. Check logs: `docker-compose logs`
4. Clear caches: `dotnet nuget locals all --clear`

The platform is ready for local development! 🎉
