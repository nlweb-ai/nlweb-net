using Microsoft.Extensions.Logging;
using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Interface for executing tools based on tool selection results.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes the appropriate tool handler for the given request and selected tool.
    /// </summary>
    /// <param name="request">The NLWeb request to process</param>
    /// <param name="selectedTool">The tool selected by the tool selector</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The processed response from the tool handler</returns>
    Task<NLWebResponse> ExecuteToolAsync(NLWebRequest request, string selectedTool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tool handlers.
    /// </summary>
    /// <returns>Collection of available tool handlers</returns>
    IEnumerable<IToolHandler> GetAvailableTools();
}

/// <summary>
/// Implementation of tool execution logic that routes requests to appropriate tool handlers.
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IEnumerable<IToolHandler> _toolHandlers;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(IEnumerable<IToolHandler> toolHandlers, ILogger<ToolExecutor> logger)
    {
        _toolHandlers = toolHandlers ?? throw new ArgumentNullException(nameof(toolHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<NLWebResponse> ExecuteToolAsync(NLWebRequest request, string selectedTool, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing tool '{Tool}' for request {QueryId}", selectedTool, request.QueryId);

        var handler = _toolHandlers
            .Where(h => h.ToolType.Equals(selectedTool, StringComparison.OrdinalIgnoreCase))
            .Where(h => h.CanHandle(request))
            .OrderByDescending(h => h.GetPriority(request))
            .FirstOrDefault();

        if (handler == null)
        {
            _logger.LogWarning("No handler found for tool '{Tool}' that can process request {QueryId}", selectedTool, request.QueryId);
            throw new InvalidOperationException($"No handler available for tool '{selectedTool}'");
        }

        _logger.LogDebug("Using handler {HandlerType} for tool '{Tool}'", handler.GetType().Name, selectedTool);

        try
        {
            var response = await handler.ExecuteAsync(request, cancellationToken);
            _logger.LogDebug("Tool '{Tool}' execution completed for request {QueryId}", selectedTool, request.QueryId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool '{Tool}' execution failed for request {QueryId}", selectedTool, request.QueryId);
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<IToolHandler> GetAvailableTools()
    {
        return _toolHandlers.ToList();
    }
}