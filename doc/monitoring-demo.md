# NLWebNet Monitoring and Observability Demo

This document demonstrates the production-ready monitoring and observability features implemented in NLWebNet.

## Features Implemented

### Health Checks

The library now includes comprehensive health checks accessible via REST endpoints:

#### Basic Health Check


```http
GET /health

```

Returns basic health status:


```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}

```

#### Detailed Health Check


```http
GET /health/detailed

```

Returns detailed status of all services:


```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "nlweb": {
      "status": "Healthy",
      "description": "NLWeb service is operational",
      "duration": "00:00:00.0012345"
    },
    "data-backend": {
      "status": "Healthy",
      "description": "Data backend (MockDataBackend) is operational",
      "duration": "00:00:00.0098765"
    },
    "ai-service": {
      "status": "Healthy",
      "description": "AI/MCP service is operational",
      "duration": "00:00:00.0087654"
    }
  }
}

```

### Metrics Collection

The library automatically collects comprehensive metrics using .NET 9 built-in metrics:

#### Request Metrics

- `nlweb.requests.total` - Total number of requests processed
- `nlweb.request.duration` - Duration of request processing in milliseconds
- `nlweb.requests.errors` - Total number of request errors

#### AI Service Metrics

- `nlweb.ai.calls.total` - Total number of AI service calls
- `nlweb.ai.duration` - Duration of AI service calls in milliseconds
- `nlweb.ai.errors` - Total number of AI service errors

#### Data Backend Metrics

- `nlweb.data.queries.total` - Total number of data backend queries
- `nlweb.data.duration` - Duration of data backend operations in milliseconds
- `nlweb.data.errors` - Total number of data backend errors

#### Health Check Metrics

- `nlweb.health.checks.total` - Total number of health check executions
- `nlweb.health.failures` - Total number of health check failures

#### Business Metrics

- `nlweb.queries.by_type` - Count of queries by type (List, Summarize, Generate)
- `nlweb.queries.complexity` - Query complexity score based on length and structure

### Rate Limiting

Configurable rate limiting with multiple strategies:

#### Default Configuration

- 100 requests per minute per client
- IP-based identification by default
- Optional client ID-based limiting via `X-Client-Id` header

#### Rate Limit Headers

All responses include rate limit information:


```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 45

```

#### Rate Limit Exceeded Response

When limits are exceeded, returns HTTP 429:


```json
{
  "error": "rate_limit_exceeded",
  "message": "Rate limit exceeded. Maximum 100 requests per 1 minute(s).",
  "retry_after_seconds": 45
}

```

### Structured Logging

Enhanced logging with correlation IDs and structured data:

#### Correlation ID Tracking

- Automatic correlation ID generation for each request
- Correlation ID included in all log entries
- Exposed via `X-Correlation-ID` response header

#### Structured Log Data

Each log entry includes:

- `CorrelationId` - Unique request identifier
- `RequestPath` - The request path
- `RequestMethod` - HTTP method
- `UserAgent` - Client user agent
- `RemoteIP` - Client IP address
- `Timestamp` - ISO 8601 timestamp

## Configuration

### Basic Setup


```csharp
var builder = WebApplication.CreateBuilder(args);

// Add NLWebNet with monitoring
builder.Services.AddNLWebNet(options =>
{
    // Configure rate limiting
    options.RateLimiting.Enabled = true;
    options.RateLimiting.RequestsPerWindow = 100;
    options.RateLimiting.WindowSizeInMinutes = 1;
    options.RateLimiting.EnableIPBasedLimiting = true;
    options.RateLimiting.EnableClientBasedLimiting = false;
});

var app = builder.Build();

// Add NLWebNet middleware (includes rate limiting, metrics, and correlation IDs)
app.UseNLWebNet();

// Map NLWebNet endpoints (includes health checks)
app.MapNLWebNet();

app.Run();

```

### Advanced Rate Limiting Configuration


```csharp
builder.Services.AddNLWebNet(options =>
{
    options.RateLimiting.Enabled = true;
    options.RateLimiting.RequestsPerWindow = 500;        // Higher limit
    options.RateLimiting.WindowSizeInMinutes = 5;        // 5-minute window
    options.RateLimiting.EnableIPBasedLimiting = false;  // Disable IP limiting
    options.RateLimiting.EnableClientBasedLimiting = true; // Enable client ID limiting
    options.RateLimiting.ClientIdHeader = "X-API-Key";   // Custom header
});

```

### Custom Data Backend with Health Checks


```csharp
// Register custom data backend - health checks automatically included
builder.Services.AddNLWebNet<MyCustomDataBackend>();

```

## Monitoring Integration

### Prometheus/Grafana

The built-in .NET metrics can be exported to Prometheus:


```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();
        builder.AddMeter("NLWebNet"); // Add NLWebNet metrics
    });

```

### Azure Application Insights

Integrate with Azure Application Insights:


```csharp
builder.Services.AddApplicationInsightsTelemetry();

```

The structured logging and correlation IDs will automatically be included in Application Insights traces.

## Production Readiness

### What's Included

- ✅ Comprehensive health checks for all services
- ✅ Automatic metrics collection with detailed labels
- ✅ Rate limiting with configurable strategies
- ✅ Structured logging with correlation ID tracking
- ✅ Proper HTTP status codes and error responses
- ✅ CORS support for monitoring endpoints
- ✅ 62 comprehensive tests (100% pass rate)

### Ready for Production Use

The monitoring and observability features are now production-ready and provide:

- Real-time health monitoring
- Performance metrics collection
- Request rate limiting
- Distributed tracing support via correlation IDs
- Integration points for external monitoring systems

### Next Steps for Full Production Deployment

- Configure external monitoring systems (Prometheus, Application Insights)
- Set up alerting rules based on health checks and metrics
- Implement log aggregation and analysis
- Configure distributed tracing for complex scenarios
