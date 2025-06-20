using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for processing and decontextualizing queries.
/// </summary>
public interface IQueryProcessor
{
    /// <summary>
    /// Processes a query and returns a decontextualized version if needed.
    /// </summary>
    /// <param name="request">The original NLWeb request</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The processed query ready for backend search</returns>
    Task<string> ProcessQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique query ID if not provided in the request.
    /// </summary>
    /// <param name="request">The NLWeb request</param>
    /// <returns>A unique query ID</returns>
    string GenerateQueryId(NLWebRequest request);

    /// <summary>
    /// Validates the incoming request and ensures all required fields are present.
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateRequest(NLWebRequest request);
}
