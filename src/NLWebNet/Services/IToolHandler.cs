using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Base interface for all tool handlers in the Advanced Tool System.
/// Tool handlers process specific types of queries based on intent analysis.
/// </summary>
public interface IToolHandler
{
    /// <summary>
    /// The tool type this handler supports (e.g., "search", "details", "compare", "ensemble", "recipe").
    /// </summary>
    string ToolType { get; }

    /// <summary>
    /// Executes the tool functionality for the given request.
    /// </summary>
    /// <param name="request">The NLWeb request to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The processed response</returns>
    Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this tool handler can process the given request.
    /// </summary>
    /// <param name="request">The request to analyze</param>
    /// <returns>True if this handler can process the request, false otherwise</returns>
    bool CanHandle(NLWebRequest request);

    /// <summary>
    /// Gets the priority of this handler for the given request.
    /// Higher values indicate higher priority.
    /// </summary>
    /// <param name="request">The request to analyze</param>
    /// <returns>Priority value (0-100)</returns>
    int GetPriority(NLWebRequest request);
}