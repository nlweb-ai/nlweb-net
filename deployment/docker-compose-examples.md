# Development with Docker Compose

This section provides examples for different Docker Compose configurations.

## Basic Development Setup

```bash
# Start the application
docker-compose up -d

# View logs
docker-compose logs -f nlwebnet

# Stop the application
docker-compose down
```

## With Monitoring Stack

```bash
# Start with Prometheus and Grafana
docker-compose --profile monitoring up -d

# Access services:
# - Application: http://localhost:8080
# - Prometheus: http://localhost:9090
# - Grafana: http://localhost:3000 (admin/admin)
```

## With Reverse Proxy

```bash
# Start with Nginx reverse proxy
docker-compose --profile with-proxy up -d

# Access via proxy: http://localhost
```

## Environment Variables

You can override configuration using environment variables:

```bash
# Example: Increase rate limiting
export NLWebNet__RateLimiting__RequestsPerWindow=2000
docker-compose up -d
```

## Production-like Setup

```bash
# Run all services
docker-compose --profile monitoring --profile with-proxy up -d
```