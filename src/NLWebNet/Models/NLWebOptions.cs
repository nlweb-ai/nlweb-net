using System.ComponentModel.DataAnnotations;
using NLWebNet.RateLimiting;

namespace NLWebNet.Models;

/// <summary>
/// Configuration options for the NLWebNet library.
/// </summary>
public class NLWebOptions
{
    /// <summary>
    /// The configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "NLWebNet";

    /// <summary>
    /// The default query mode to use when none is specified in requests.
    /// </summary>
    public QueryMode DefaultMode { get; set; } = QueryMode.List;

    /// <summary>
    /// Whether streaming responses are enabled by default.
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Default timeout for processing requests in seconds.
    /// </summary>
    [Range(1, 300)]
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of results to return per query.
    /// </summary>
    [Range(1, 1000)]
    public int MaxResultsPerQuery { get; set; } = 50;

    /// <summary>
    /// Whether to enable query decontextualization.
    /// </summary>
    public bool EnableDecontextualization { get; set; } = true;

    /// <summary>
    /// Maximum length for query strings.
    /// </summary>
    [Range(1, 10000)]
    public int MaxQueryLength { get; set; } = 2000;

    /// <summary>
    /// Whether to enable detailed logging for debugging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Default site identifier when none is specified.
    /// </summary>
    public string? DefaultSite { get; set; }

    /// <summary>
    /// Whether to enable response caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Multi-backend configuration options. When enabled, overrides single backend behavior.
    /// </summary>
    public MultiBackendOptions MultiBackend { get; set; } = new();

    /// <summary>
    /// Whether to enable tool selection framework for routing queries to appropriate tools.
    /// When disabled, maintains existing behavior for backward compatibility.
    /// </summary>
    public bool ToolSelectionEnabled { get; set; } = false;

    /// <summary>
    /// Configuration format options for YAML and XML support.
    /// </summary>
    public ConfigurationFormatOptions ConfigurationFormat { get; set; } = new();
}
