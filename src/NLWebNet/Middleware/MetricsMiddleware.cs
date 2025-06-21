using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLWebNet.Metrics;
using System.Diagnostics;

namespace NLWebNet.Middleware;

/// <summary>
/// Middleware for collecting metrics on HTTP requests and supporting distributed tracing
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;
    private static readonly ActivitySource ActivitySource = new(NLWebMetrics.MeterName, NLWebMetrics.Version);

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

        // Start an activity for distributed tracing
        using var activity = ActivitySource.StartActivity($"{method} {path}");
        activity?.SetTag("http.method", method);
        activity?.SetTag("http.route", path);
        activity?.SetTag("http.scheme", context.Request.Scheme);

        // Add correlation ID to activity if present
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            activity?.SetTag("nlweb.correlation_id", correlationId.FirstOrDefault());
        }

        try
        {
            await _next(context);

            // Set success status
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            activity?.SetStatus(context.Response.StatusCode >= 400 ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            // Record error metrics
            NLWebMetrics.RequestErrors.Add(1,
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Endpoint, path),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.Method, method),
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.ErrorType, ex.GetType().Name));

            // Record error in activity
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("error.type", ex.GetType().Name);
                activity.SetTag("error.message", ex.Message);
            }

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

            // Add duration to activity
            activity?.SetTag("http.request.duration_ms", duration);

            _logger.LogDebug("Request {Method} {Path} completed in {Duration}ms with status {StatusCode}",
                method, path, duration, statusCode);
        }
    }
}