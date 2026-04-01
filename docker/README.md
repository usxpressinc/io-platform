# Docker Development Environment

This folder contains everything needed for local Docker development of the IO Platform.

## Quick Start

```bash
cd docker
docker-compose up -d
```

## What's Included

### Infrastructure Services
- **MongoDB** (27017) - Database
- **Kafka + Zookeeper** (9092, 2181) - Message streaming
- **OpenTelemetry Collector** (4317, 4318, 8888, 8889) - Observability
- **Prometheus** (9090) - Metrics collection
- **Grafana** (3000) - Visualization (admin/admin123)

### Application Services
- **IO.Proxy** (8080) - Public API gateway with X-Auth
- **IO.Common** (8081) - Email API (internal)
- **IO.Cass** (8082) - Carrier validation (internal)
- **IO.Elsa** (8083) - Price lookup (internal)
- **Nginx** (80, 443) - Reverse proxy for subdomain routing

## API Endpoints

### Public (via IO.Proxy)
- `POST http://localhost:8080/api/gateway/email/send` - Send email
- `POST http://localhost:8080/api/gateway/carriers/valid` - Validate carrier
- `POST http://localhost:8080/api/gateway/price/lookup` - Price lookup

### Internal (direct access)
- `http://localhost:8081` - IO.Common (behind firewall)
- `http://localhost:8082` - IO.Cass (behind firewall)
- `http://localhost:8083` - IO.Elsa (behind firewall)

## Environment Files

Create these files in the `docker/` directory:

- `.env.io-proxy` - Proxy environment variables
- `.env.io-common` - Common service environment variables
- `.env.io-cass` - Cass service environment variables
- `.env.io-elsa` - Elsa service environment variables

## Development Workflow

1. Make changes to source code
2. Run `docker-compose up -d --build` to rebuild and restart
3. Check logs: `docker-compose logs -f [service-name]`
4. Stop services: `docker-compose down`

## Monitoring

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Health checks**: 
  - http://localhost:8080/health (Proxy)
  - http://localhost:8081/health (Common)
  - http://localhost:8082/health (Cass)
  - http://localhost:8083/health (Elsa)

## Production Deployment

For production deployment, use the main `Dockerfile` in the repository root with your CI/CD pipeline. The Docker Compose setup is for local development only.
