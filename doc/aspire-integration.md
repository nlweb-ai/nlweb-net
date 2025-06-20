# .NET Aspire Integration for NLWebNet

This document describes how to use .NET Aspire as the preferred containerization and orchestration approach for the NLWebNet demo application.

## Overview

.NET Aspire is Microsoft's opinionated stack for building observable, production-ready cloud-native applications. It provides:

- **Service Discovery**: Automatic service location and communication
- **Observability**: Built-in telemetry, metrics, and health checks
- **Resilience**: Circuit breakers, retries, and timeout policies
- **Configuration**: Centralized configuration management
- **Local Development**: Aspire dashboard for debugging and monitoring

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- .NET Aspire workload: `dotnet workload install aspire`

### Running with Aspire

1. **Start the Aspire AppHost** (recommended for development):
   ```bash
   cd demo-apphost
   dotnet run
   ```

2. **Access the Aspire Dashboard** at `https://localhost:15888`
   - View application health and metrics
   - Monitor telemetry and logs
   - Debug service communication

3. **Access the NLWebNet Demo** at `http://localhost:8080`

### Project Structure

```
├── demo-apphost/              # Aspire orchestration host
│   ├── Program.cs            # Application composition
│   └── NLWebNet.Demo.AppHost.csproj
├── demo/                     # NLWebNet demo app
│   ├── Program.cs           # Aspire service defaults integration
│   └── NLWebNet.Demo.csproj
└── shared/ServiceDefaults/   # Shared Aspire configurations
    ├── Extensions.cs         # Service defaults implementation
    └── ServiceDefaults.csproj
```

## Key Features

### Service Defaults

The `ServiceDefaults` project provides common functionality:

- **OpenTelemetry**: Distributed tracing and metrics
- **Health Checks**: Application health monitoring
- **Service Discovery**: Automatic service location
- **Resilience**: HTTP retry policies and circuit breakers

### Observability

Aspire automatically instruments the application with:

- **Distributed Tracing**: Request flow across services
- **Metrics**: Performance and usage statistics
- **Logging**: Structured application logs
- **Health Checks**: Application and dependency status

### Development Experience

- **Hot Reload**: Automatic restart on code changes
- **Dashboard**: Visual monitoring and debugging
- **Service Map**: Visualize service dependencies
- **Resource Management**: Automatic service lifecycle

## Deployment Options

### Local Development

```bash
# Run with Aspire orchestration
cd demo-apphost
dotnet run

# Run standalone (without Aspire dashboard)
cd demo
dotnet run
```

### Container Deployment

The demo app can be containerized while maintaining Aspire benefits:

```dockerfile
# The existing Dockerfile works with Aspire-enabled apps
docker build -t nlwebnet-demo .
docker run -p 8080:8080 nlwebnet-demo
```

### Cloud Deployment

Aspire apps can be deployed to:

- **Azure Container Apps**: Native Aspire support
- **Kubernetes**: Using Aspire manifest generation
- **Docker Compose**: Generated from Aspire configuration

## Configuration

### Environment Variables

```bash
# Application configuration
NLWebNet__DefaultMode=List
NLWebNet__EnableStreaming=true
NLWebNet__DefaultTimeoutSeconds=30
NLWebNet__MaxResultsPerQuery=50

# OpenTelemetry configuration
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-otlp-endpoint
OTEL_SERVICE_NAME=nlwebnet-demo
```

### Health Checks

Aspire automatically configures health check endpoints:

- `/health` - Overall application health
- `/alive` - Liveness probe
- `/health/ready` - Readiness probe

## Advantages over Traditional Docker

### Development Experience
- **Integrated Dashboard**: Visual monitoring and debugging
- **Service Discovery**: No manual endpoint configuration
- **Automatic Restart**: Hot reload support
- **Centralized Logging**: All services in one view

### Production Benefits
- **Built-in Observability**: No additional instrumentation needed
- **Standardized Patterns**: Consistent health checks and metrics
- **Resilience**: Automatic retry and circuit breaker patterns
- **Configuration Management**: Environment-specific settings

### Operational Excellence
- **Health Monitoring**: Comprehensive health check strategy
- **Performance Metrics**: Built-in performance monitoring
- **Distributed Tracing**: Request flow visualization
- **Resource Management**: Automatic resource lifecycle

## Migration from Docker Compose

Aspire can replace Docker Compose for local development:

**Before (docker-compose.yml):**
```yaml
services:
  nlwebnet-demo:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

**After (AppHost Program.cs):**
```csharp
var nlwebnetDemo = builder.AddProject("nlwebnet-demo", "../demo/NLWebNet.Demo.csproj")
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);
```

## Troubleshooting

### Common Issues

1. **Dashboard not accessible**: Ensure no firewall blocking port 15888
2. **Service discovery failing**: Check project references in AppHost
3. **Health checks failing**: Verify health endpoint implementation

### Debugging

Use the Aspire dashboard to:
- View service logs in real-time
- Monitor health check status
- Trace request flows
- Analyze performance metrics

## Best Practices

1. **Use Service Defaults**: Always reference ServiceDefaults project
2. **Environment Configuration**: Use environment-specific settings
3. **Health Checks**: Implement meaningful health checks
4. **Observability**: Leverage built-in telemetry
5. **Resource Naming**: Use consistent naming conventions

## Next Steps

- Explore multi-service scenarios with databases and message queues
- Configure production observability with Azure Monitor or Jaeger
- Implement custom health checks for external dependencies
- Set up continuous deployment with Aspire manifest generation