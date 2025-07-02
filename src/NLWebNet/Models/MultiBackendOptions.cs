using System.ComponentModel.DataAnnotations;

namespace NLWebNet.Models;

/// <summary>
/// Configuration options for multi-backend retrieval architecture.
/// </summary>
public class MultiBackendOptions
{
    /// <summary>
    /// The configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "NLWebNet:MultiBackend";

    /// <summary>
    /// Whether multi-backend mode is enabled. When false, uses single backend for backward compatibility.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The identifier of the backend to use as the primary write endpoint.
    /// </summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>
    /// Configuration for individual backend endpoints.
    /// </summary>
    public Dictionary<string, BackendEndpointOptions> Endpoints { get; set; } = new();

    /// <summary>
    /// Whether to enable parallel querying across backends.
    /// </summary>
    public bool EnableParallelQuerying { get; set; } = true;

    /// <summary>
    /// Whether to enable automatic result deduplication.
    /// </summary>
    public bool EnableResultDeduplication { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent backend queries.
    /// </summary>
    [Range(1, 10)]
    public int MaxConcurrentQueries { get; set; } = 5;

    /// <summary>
    /// Timeout for individual backend queries in seconds.
    /// </summary>
    [Range(1, 120)]
    public int BackendTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration for an individual backend endpoint.
/// </summary>
public class BackendEndpointOptions
{
    /// <summary>
    /// Whether this backend endpoint is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The type of backend (e.g., "azure_ai_search", "mock", "custom").
    /// </summary>
    public string BackendType { get; set; } = string.Empty;

    /// <summary>
    /// Priority for this backend (higher values = higher priority).
    /// Used for ordering results when deduplication is disabled.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Backend-specific configuration properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}