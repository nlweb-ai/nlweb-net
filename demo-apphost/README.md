# NLWebNet Demo - Aspire AppHost

This project contains the .NET Aspire orchestration host for the NLWebNet demo application.

## Quick Start

```bash
# Run the Aspire orchestrated application
dotnet run

# Access the Aspire dashboard
# Open https://localhost:15888 in your browser

# Access the NLWebNet demo application
# Open http://localhost:8080 in your browser
```

## What is Aspire AppHost?

The AppHost project serves as the orchestration center for the NLWebNet demo application when using .NET Aspire. It:

- **Orchestrates Services**: Manages the lifecycle of the demo application
- **Provides Configuration**: Sets environment variables and connection strings
- **Enables Observability**: Automatically instruments the application with telemetry
- **Offers Development Tools**: Provides the Aspire dashboard for monitoring and debugging

## Features

### Service Orchestration
- Automatically starts and manages the NLWebNet demo application
- Configures networking and service discovery
- Handles environment-specific configurations

### Observability
- **Distributed Tracing**: Track requests across the application
- **Metrics Collection**: Monitor performance and usage statistics
- **Health Monitoring**: Real-time health check status
- **Structured Logging**: Centralized log aggregation

### Development Experience
- **Aspire Dashboard**: Visual interface for monitoring and debugging
- **Hot Reload**: Automatic application restart on code changes
- **Service Map**: Visualize application architecture
- **Resource Management**: Automatic cleanup and lifecycle management

## Configuration

The AppHost configures the demo application with:

```csharp
var nlwebnetDemo = builder.AddProject("nlwebnet-demo", "../demo/NLWebNet.Demo.csproj")
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("NLWebNet__DefaultMode", "List")
    .WithEnvironment("NLWebNet__EnableStreaming", "true")
    .WithEnvironment("NLWebNet__DefaultTimeoutSeconds", "30")
    .WithEnvironment("NLWebNet__MaxResultsPerQuery", "50");
```

## Project Structure

```
demo-apphost/
├── Program.cs                 # Aspire orchestration configuration
├── appsettings.json          # AppHost configuration
├── appsettings.Development.json  # Development-specific settings
└── NLWebNet.Demo.AppHost.csproj  # Project file with Aspire references
```

## Dependencies

- **Aspire.Hosting**: Core Aspire orchestration framework
- **ServiceDefaults**: Shared service configuration and observability
- **NLWebNet.Demo**: The demo application being orchestrated

## Usage Scenarios

### Local Development
- Full observability stack with dashboard
- Automatic service discovery
- Hot reload and debugging support

### Testing
- Consistent environment setup
- Integrated health checks
- Performance monitoring

### Production Readiness
- Standard health check endpoints
- Built-in telemetry and monitoring
- Resilience patterns (retry, circuit breaker)

## Next Steps

1. **Extend the Application**: Add databases, message queues, or additional services
2. **Custom Observability**: Configure Azure Monitor or other observability providers
3. **Deployment**: Use Aspire for generating deployment manifests for Kubernetes or Azure Container Apps
4. **Integration Testing**: Leverage Aspire for integration test scenarios

For more information, see the [complete Aspire integration guide](../doc/aspire-integration.md).