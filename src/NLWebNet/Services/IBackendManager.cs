using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for managing multiple data backends and coordinating operations across them.
/// </summary>
public interface IBackendManager
{
    /// <summary>
    /// Searches across all enabled backends in parallel, with automatic result deduplication.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="site">Optional site filter to restrict search scope</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A collection of deduplicated search results with relevance scores</returns>
    Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available sites/scopes from all backends.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A collection of all available site identifiers across all backends</returns>
    Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific item by its URL or ID from all backends.
    /// Returns the first match found across all backends.
    /// </summary>
    /// <param name="url">The URL or identifier of the item</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The detailed item information, or null if not found in any backend</returns>
    Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary write backend for operations that require a single endpoint.
    /// </summary>
    /// <returns>The primary write backend, or null if not configured</returns>
    IDataBackend? GetWriteBackend();

    /// <summary>
    /// Gets information about all configured backends and their capabilities.
    /// </summary>
    /// <returns>A collection of backend information including their capabilities</returns>
    IEnumerable<BackendInfo> GetBackendInfo();
}

/// <summary>
/// Information about a configured backend.
/// </summary>
public record BackendInfo
{
    /// <summary>
    /// The unique identifier for this backend.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Whether this backend is currently enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Whether this is the designated write endpoint.
    /// </summary>
    public bool IsWriteEndpoint { get; init; }

    /// <summary>
    /// The backend's capabilities.
    /// </summary>
    public BackendCapabilities Capabilities { get; init; } = new();

    /// <summary>
    /// The priority of this backend.
    /// </summary>
    public int Priority { get; init; }
}