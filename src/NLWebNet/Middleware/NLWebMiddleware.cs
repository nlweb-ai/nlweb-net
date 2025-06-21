using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NLWebNet.Middleware;

/// <summary>
/// Middleware for handling NLWeb-specific concerns like query ID generation and error handling.
/// </summary>
public class NLWebMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NLWebMiddleware> _logger;

    public NLWebMiddleware(RequestDelegate next, ILogger<NLWebMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add correlation ID for request tracking
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();

        // Store correlation ID in items for other middleware/services to use
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Create logging scope with correlation ID
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.Value ?? "unknown",
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
            ["RemoteIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        });

        // Log incoming request with structured data
        _logger.LogInformation("Processing {Method} {Path} from {RemoteIP} with correlation ID {CorrelationId}",
            context.Request.Method, context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", correlationId);

        try
        {
            // Add CORS headers for NLWeb endpoints
            if (context.Request.Path.StartsWithSegments("/ask") ||
                context.Request.Path.StartsWithSegments("/mcp") ||
                context.Request.Path.StartsWithSegments("/health"))
            {
                AddCorsHeaders(context);
            }

            await _next(context);

            // Log successful completion
            _logger.LogInformation("Request completed successfully with status {StatusCode}",
                context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in NLWeb middleware for {Path} with correlation ID {CorrelationId}",
                context.Request.Path, correlationId);

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static void AddCorsHeaders(HttpContext context)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Correlation-ID, X-Client-Id");
        context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Correlation-ID, X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset");
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = new
        {
            title = "Internal Server Error",
            detail = "An unexpected error occurred",
            status = StatusCodes.Status500InternalServerError,
            traceId = context.TraceIdentifier,
            correlationId = correlationId,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
