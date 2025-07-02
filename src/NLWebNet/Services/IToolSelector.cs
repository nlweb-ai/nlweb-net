using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for selecting appropriate tools based on query intent.
/// </summary>
public interface IToolSelector
{
    /// <summary>
    /// Selects the appropriate tool based on the query intent and context.
    /// </summary>
    /// <param name="request">The NLWeb request containing the query and context</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The selected tool name, or null if no specific tool is needed</returns>
    Task<string?> SelectToolAsync(NLWebRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if tool selection is needed for the given request.
    /// </summary>
    /// <param name="request">The NLWeb request to analyze</param>
    /// <returns>True if tool selection should be performed, false otherwise</returns>
    bool ShouldSelectTool(NLWebRequest request);
}