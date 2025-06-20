using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Main service interface for processing NLWeb requests and generating responses.
/// </summary>
public interface INLWebService
{
    /// <summary>
    /// Processes an NLWeb request and returns the appropriate response.
    /// </summary>
    /// <param name="request">The NLWeb request to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The processed NLWeb response</returns>
    Task<NLWebResponse> ProcessRequestAsync(NLWebRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an NLWeb request and returns a streaming response.
    /// </summary>
    /// <param name="request">The NLWeb request to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An async enumerable of response chunks for streaming</returns>
    IAsyncEnumerable<NLWebResponse> ProcessRequestStreamAsync(NLWebRequest request, CancellationToken cancellationToken = default);
}
