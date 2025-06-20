using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLWebNet.Metrics;
using System.Diagnostics;

namespace NLWebNet.Middleware;

/// <summary>
/// Middleware for collecting metrics on HTTP requests
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Record error metrics
            NLWebMetrics.RequestErrors.Add(1, 
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Endpoint, path),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Method, method),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.ErrorType, ex.GetType().Name));

            _logger.LogError(ex, "Request failed for {Method} {Path}", method, path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode.ToString();

            // Record request metrics
            NLWebMetrics.RequestCount.Add(1,
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Endpoint, path),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Method, method),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.StatusCode, statusCode));

            NLWebMetrics.RequestDuration.Record(duration,
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Endpoint, path),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Method, method),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.StatusCode, statusCode));

            _logger.LogDebug("Request {Method} {Path} completed in {Duration}ms with status {StatusCode}",
                method, path, duration, statusCode);
        }
    }
}