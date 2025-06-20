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
        
        context.Response.Headers.Append("X-Correlation-ID", correlationId);
        
        // Log incoming request
        _logger.LogDebug("Processing {Method} {Path} with correlation ID {CorrelationId}", 
            context.Request.Method, context.Request.Path, correlationId);

        try
        {
            // Add CORS headers for NLWeb endpoints
            if (context.Request.Path.StartsWithSegments("/ask") || 
                context.Request.Path.StartsWithSegments("/mcp"))
            {
                AddCorsHeaders(context);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in NLWeb middleware for {Path} with correlation ID {CorrelationId}", 
                context.Request.Path, correlationId);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static void AddCorsHeaders(HttpContext context)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Correlation-ID");
        context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Correlation-ID");
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var problemDetails = new
        {
            title = "Internal Server Error",
            detail = "An unexpected error occurred",
            status = StatusCodes.Status500InternalServerError,
            traceId = context.TraceIdentifier
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
