using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.RateLimiting;
using System.Text.Json;

namespace NLWebNet.Middleware;

/// <summary>
/// Middleware for enforcing rate limits on requests
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitingService rateLimitingService,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _rateLimitingService = rateLimitingService ?? throw new ArgumentNullException(nameof(rateLimitingService));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var identifier = GetClientIdentifier(context);
        var isAllowed = await _rateLimitingService.IsRequestAllowedAsync(identifier);

        if (!isAllowed)
        {
            await HandleRateLimitExceeded(context, identifier);
            return;
        }

        // Add rate limit headers
        var status = await _rateLimitingService.GetRateLimitStatusAsync(identifier);
        context.Response.Headers.Append("X-RateLimit-Limit", _options.RequestsPerWindow.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", status.RequestsRemaining.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", ((int)status.WindowResetTime.TotalSeconds).ToString());

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try client ID header first if enabled
        if (_options.EnableClientBasedLimiting)
        {
            var clientId = context.Request.Headers[_options.ClientIdHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientId))
            {
                return $"client:{clientId}";
            }
        }

        // Fall back to IP-based limiting if enabled
        if (_options.EnableIPBasedLimiting)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ip}";
        }

        // Default fallback
        return "default";
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string identifier)
    {
        var status = await _rateLimitingService.GetRateLimitStatusAsync(identifier);

        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.Headers.Append("X-RateLimit-Limit", _options.RequestsPerWindow.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", "0");
        context.Response.Headers.Append("X-RateLimit-Reset", ((int)status.WindowResetTime.TotalSeconds).ToString());
        context.Response.Headers.Append("Retry-After", ((int)status.WindowResetTime.TotalSeconds).ToString());

        var response = new
        {
            error = "rate_limit_exceeded",
            message = $"Rate limit exceeded. Maximum {_options.RequestsPerWindow} requests per {_options.WindowSizeInMinutes} minute(s).",
            retry_after_seconds = (int)status.WindowResetTime.TotalSeconds
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));

        _logger.LogWarning("Rate limit exceeded for identifier {Identifier}. Requests: {Requests}/{Limit}",
            identifier, status.TotalRequests, _options.RequestsPerWindow);
    }
}