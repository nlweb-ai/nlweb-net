using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Implementation of tool selection logic that routes queries to appropriate tools based on intent.
/// </summary>
public class ToolSelector : IToolSelector
{
    private readonly ILogger<ToolSelector> _logger;
    private readonly NLWebOptions _options;

    public ToolSelector(ILogger<ToolSelector> logger, IOptions<NLWebOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<string?> SelectToolAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        if (!ShouldSelectTool(request))
        {
            _logger.LogDebug("Tool selection not needed for request {QueryId}", request.QueryId);
            return null;
        }

        _logger.LogDebug("Selecting tool for query: {Query}", request.Query);

        // Simple intent-based tool selection
        // In a full implementation, this would use more sophisticated intent analysis
        var selectedTool = await AnalyzeQueryIntentAsync(request.Query, cancellationToken);

        _logger.LogDebug("Selected tool: {Tool} for query {QueryId}", selectedTool ?? "none", request.QueryId);
        return selectedTool;
    }

    /// <inheritdoc />
    public bool ShouldSelectTool(NLWebRequest request)
    {
        // Don't perform tool selection if:
        // 1. Tool selection is disabled in configuration
        // 2. Generate mode is used (maintain existing behavior)
        // 3. Request already has a decontextualized query (already processed)
        
        if (!_options.ToolSelectionEnabled)
        {
            return false;
        }

        if (request.Mode == QueryMode.Generate)
        {
            _logger.LogDebug("Skipping tool selection for Generate mode to maintain existing behavior");
            return false;
        }

        if (!string.IsNullOrEmpty(request.DecontextualizedQuery))
        {
            _logger.LogDebug("Skipping tool selection for request with decontextualized query");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Analyzes the query intent to determine the appropriate tool.
    /// This is a simplified implementation - production would use more sophisticated NLP.
    /// </summary>
    private Task<string?> AnalyzeQueryIntentAsync(string query, CancellationToken cancellationToken)
    {
        var queryLower = query.ToLowerInvariant();

        // Basic keyword-based intent detection
        // In production, this would use ML models or more sophisticated analysis
        
        if (ContainsKeywords(queryLower, "search", "find", "look for", "locate"))
        {
            return Task.FromResult<string?>("search");
        }

        if (ContainsKeywords(queryLower, "compare", "difference", "versus", "vs", "contrast"))
        {
            return Task.FromResult<string?>("compare");
        }

        if (ContainsKeywords(queryLower, "details", "information about", "tell me about", "describe"))
        {
            return Task.FromResult<string?>("details");
        }

        if (ContainsKeywords(queryLower, "recommend", "suggest", "what should", "ensemble", "set of"))
        {
            return Task.FromResult<string?>("ensemble");
        }

        // Default to search tool for general queries
        return Task.FromResult<string?>("search");
    }

    private static bool ContainsKeywords(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }
}