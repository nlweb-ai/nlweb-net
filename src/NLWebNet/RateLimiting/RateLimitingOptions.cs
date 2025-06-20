namespace NLWebNet.RateLimiting;

/// <summary>
/// Configuration options for NLWebNet rate limiting
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of requests per window
    /// </summary>
    public int RequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting in minutes
    /// </summary>
    public int WindowSizeInMinutes { get; set; } = 1;

    /// <summary>
    /// Whether to use IP-based rate limiting
    /// </summary>
    public bool EnableIPBasedLimiting { get; set; } = true;

    /// <summary>
    /// Whether to use client ID-based rate limiting
    /// </summary>
    public bool EnableClientBasedLimiting { get; set; } = false;

    /// <summary>
    /// Custom client identifier header name
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-Client-Id";
}