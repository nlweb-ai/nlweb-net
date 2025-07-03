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

    /// <summary>
    /// Constants for tool names and associated keywords
    /// </summary>
    public static class ToolConstants
    {
        // Tool names
        public const string SearchTool = "search";
        public const string CompareTool = "compare";
        public const string DetailsTool = "details";
        public const string EnsembleTool = "ensemble";

        // Keywords for each tool
        public static readonly string[] SearchKeywords = { "search", "find", "look for", "locate" };
        public static readonly string[] CompareKeywords = { "compare", "difference", "versus", "vs", "contrast" };
        public static readonly string[] DetailsKeywords = { "details", "information about", "tell me about", "describe" };
        public static readonly string[] EnsembleKeywords = { "recommend", "suggest", "what should", "ensemble", "set of" };
    }

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

        if (ContainsKeywords(queryLower, ToolConstants.SearchKeywords))
        {
            return Task.FromResult<string?>(ToolConstants.SearchTool);
        }

        if (ContainsKeywords(queryLower, ToolConstants.CompareKeywords))
        {
            return Task.FromResult<string?>(ToolConstants.CompareTool);
        }

        if (ContainsKeywords(queryLower, ToolConstants.DetailsKeywords))
        {
            return Task.FromResult<string?>(ToolConstants.DetailsTool);
        }

        if (ContainsKeywords(queryLower, ToolConstants.EnsembleKeywords))
        {
            return Task.FromResult<string?>(ToolConstants.EnsembleTool);
        }

        // Default to search tool for general queries
        return Task.FromResult<string?>(ToolConstants.SearchTool);
    }

    private static bool ContainsKeywords(string text, string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }
}