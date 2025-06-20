using Microsoft.AspNetCore.Http;

namespace NLWebNet.Utilities;

/// <summary>
/// Utility class for correlation ID management
/// </summary>
public static class CorrelationIdUtility
{
    /// <summary>
    /// Gets the correlation ID from the current HTTP context
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>The correlation ID, or "unknown" if not found</returns>
    public static string GetCorrelationId(HttpContext? httpContext)
    {
        if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        {
            return correlationId?.ToString() ?? "unknown";
        }

        // Fallback to header if not in items
        if (httpContext?.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValue) == true)
        {
            return headerValue.FirstOrDefault() ?? "unknown";
        }

        return "unknown";
    }

    /// <summary>
    /// Creates structured logging properties with correlation ID and request context
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="additionalProperties">Additional properties to include</param>
    /// <returns>Dictionary of properties for structured logging</returns>
    public static Dictionary<string, object> CreateLoggingProperties(HttpContext? httpContext, Dictionary<string, object>? additionalProperties = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["CorrelationId"] = GetCorrelationId(httpContext),
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (httpContext != null)
        {
            properties["RequestPath"] = httpContext.Request.Path.Value ?? "unknown";
            properties["RequestMethod"] = httpContext.Request.Method;
            properties["UserAgent"] = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
            properties["RemoteIP"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        if (additionalProperties != null)
        {
            foreach (var (key, value) in additionalProperties)
            {
                properties[key] = value;
            }
        }

        return properties;
    }
}