using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for generating results based on different query modes (list, summarize, generate).
/// </summary>
public interface IResultGenerator
{
    /// <summary>
    /// Generates a list of results from the backend data.
    /// </summary>
    /// <param name="query">The processed query</param>
    /// <param name="site">Optional site filter</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A list of NLWeb results</returns>
    Task<IEnumerable<NLWebResult>> GenerateListAsync(string query, string? site = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a summary based on the search results.
    /// </summary>
    /// <param name="query">The processed query</param>
    /// <param name="results">The list of results to summarize</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A summary string and the original results</returns>
    Task<(string Summary, IEnumerable<NLWebResult> Results)> GenerateSummaryAsync(string query, IEnumerable<NLWebResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an AI-powered response based on the search results (RAG-style).
    /// </summary>
    /// <param name="query">The processed query</param>
    /// <param name="results">The list of results to use as context</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A generated response and the source results</returns>
    Task<(string GeneratedResponse, IEnumerable<NLWebResult> Results)> GenerateResponseAsync(string query, IEnumerable<NLWebResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates streaming results for real-time response generation.
    /// </summary>
    /// <param name="query">The processed query</param>
    /// <param name="results">The list of results to use as context</param>
    /// <param name="mode">The generation mode</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An async enumerable of response chunks</returns>
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(string query, IEnumerable<NLWebResult> results, QueryMode mode, CancellationToken cancellationToken = default);
}
