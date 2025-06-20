using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for pluggable data backends that provide search and retrieval functionality.
/// </summary>
public interface IDataBackend
{
    /// <summary>
    /// Searches the backend data store for relevant results.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="site">Optional site filter to restrict search scope</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A collection of search results with relevance scores</returns>
    Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available sites/scopes in the backend.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A collection of available site identifiers</returns>
    Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific item by its URL or ID.
    /// </summary>
    /// <param name="url">The URL or identifier of the item</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The detailed item information, or null if not found</returns>
    Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the backend's capabilities and configuration.
    /// </summary>
    /// <returns>Information about what the backend supports</returns>
    BackendCapabilities GetCapabilities();
}

/// <summary>
/// Describes the capabilities of a data backend.
/// </summary>
public record BackendCapabilities
{
    /// <summary>
    /// Whether the backend supports site-based filtering.
    /// </summary>
    public bool SupportsSiteFiltering { get; init; } = false;

    /// <summary>
    /// Whether the backend supports full-text search.
    /// </summary>
    public bool SupportsFullTextSearch { get; init; } = true;

    /// <summary>
    /// Whether the backend supports semantic/vector search.
    /// </summary>
    public bool SupportsSemanticSearch { get; init; } = false;

    /// <summary>
    /// Maximum number of results the backend can return.
    /// </summary>
    public int MaxResults { get; init; } = 100;

    /// <summary>
    /// Description of the backend implementation.
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
