using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace NLWebNet.RateLimiting;

/// <summary>
/// Interface for rate limiting services
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Checks if a request is allowed for the given identifier
    /// </summary>
    /// <param name="identifier">The client identifier (IP, user ID, etc.)</param>
    /// <returns>True if the request is allowed, false if rate limited</returns>
    Task<bool> IsRequestAllowedAsync(string identifier);

    /// <summary>
    /// Gets the current rate limit status for an identifier
    /// </summary>
    /// <param name="identifier">The client identifier</param>
    /// <returns>Rate limit status information</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier);
}

/// <summary>
/// Rate limit status information
/// </summary>
public class RateLimitStatus
{
    public bool IsAllowed { get; set; }
    public int RequestsRemaining { get; set; }
    public TimeSpan WindowResetTime { get; set; }
    public int TotalRequests { get; set; }
}

/// <summary>
/// Simple in-memory rate limiting service using token bucket algorithm
/// </summary>
public class InMemoryRateLimitingService : IRateLimitingService
{
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();

    public InMemoryRateLimitingService(IOptions<RateLimitingOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<bool> IsRequestAllowedAsync(string identifier)
    {
        if (!_options.Enabled)
            return Task.FromResult(true);

        var bucket = GetOrCreateBucket(identifier);
        var now = DateTime.UtcNow;

        lock (bucket)
        {
            // Reset bucket if window has passed
            if (now >= bucket.WindowStart.AddMinutes(_options.WindowSizeInMinutes))
            {
                bucket.Requests = 0;
                bucket.WindowStart = now;
            }

            if (bucket.Requests < _options.RequestsPerWindow)
            {
                bucket.Requests++;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    public Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(new RateLimitStatus
            {
                IsAllowed = true,
                RequestsRemaining = int.MaxValue,
                WindowResetTime = TimeSpan.Zero,
                TotalRequests = 0
            });
        }

        var bucket = GetOrCreateBucket(identifier);
        var now = DateTime.UtcNow;

        lock (bucket)
        {
            var windowEnd = bucket.WindowStart.AddMinutes(_options.WindowSizeInMinutes);
            var resetTime = windowEnd > now ? windowEnd - now : TimeSpan.Zero;

            return Task.FromResult(new RateLimitStatus
            {
                IsAllowed = bucket.Requests < _options.RequestsPerWindow,
                RequestsRemaining = Math.Max(0, _options.RequestsPerWindow - bucket.Requests),
                WindowResetTime = resetTime,
                TotalRequests = bucket.Requests
            });
        }
    }

    private RateLimitBucket GetOrCreateBucket(string identifier)
    {
        return _buckets.GetOrAdd(identifier, _ => new RateLimitBucket
        {
            Requests = 0,
            WindowStart = DateTime.UtcNow
        });
    }

    private class RateLimitBucket
    {
        public int Requests { get; set; }
        public DateTime WindowStart { get; set; }
    }
}