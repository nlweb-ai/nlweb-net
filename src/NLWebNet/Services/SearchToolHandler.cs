using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;

namespace NLWebNet.Services;

/// <summary>
/// Tool handler for enhanced keyword and semantic search operations.
/// This is an upgrade of the current search capability with enhanced features.
/// </summary>
public class SearchToolHandler : BaseToolHandler
{
    public SearchToolHandler(
        ILogger<SearchToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    /// <inheritdoc />
    public override string ToolType => "search";

    /// <inheritdoc />
    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Executing search tool for query: {Query}", request.Query);

            // Process query for enhanced search
            var processedQuery = await QueryProcessor.ProcessQueryAsync(request, cancellationToken);
            var enhancedQuery = await OptimizeSearchQuery(processedQuery, cancellationToken);

            // Generate search results using the existing result generator
            var searchResults = await ResultGenerator.GenerateListAsync(enhancedQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();

            // Enhance results for search-specific improvements
            var enhancedResults = EnhanceSearchResults(resultsList, request.Query);

            stopwatch.Stop();

            var response = CreateSuccessResponse(request, enhancedResults, stopwatch.ElapsedMilliseconds);
            response.ProcessedQuery = enhancedQuery;
            response.Summary = $"Enhanced search completed - found {enhancedResults.Count} results";

            Logger.LogDebug("Search tool completed in {ElapsedMs}ms with {ResultCount} results",
                stopwatch.ElapsedMilliseconds, enhancedResults.Count);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse(request, $"Search tool execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override bool CanHandle(NLWebRequest request)
    {
        if (!base.CanHandle(request))
            return false;

        // Search tool can handle most general queries
        // It's designed as a fallback for general search operations
        return true;
    }

    /// <inheritdoc />
    public override int GetPriority(NLWebRequest request)
    {
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;

        // Higher priority for explicit search terms
        if (ContainsSearchKeywords(query))
            return 80;

        // Medium priority for general queries (search is often the default)
        return 60;
    }

    /// <summary>
    /// Optimizes the search query for better results.
    /// </summary>
    private Task<string> OptimizeSearchQuery(string query, CancellationToken cancellationToken)
    {
        // Basic query optimization - in production this could use ML models
        var optimized = query.Trim();

        // Remove redundant search terms
        var searchTerms = new[] { "search for", "find", "look for", "locate" };
        foreach (var term in searchTerms)
        {
            if (optimized.StartsWith(term, StringComparison.OrdinalIgnoreCase))
            {
                optimized = optimized.Substring(term.Length).Trim();
                break;
            }
        }

        return Task.FromResult(optimized);
    }

    /// <summary>
    /// Enhances search results with search-specific improvements.
    /// </summary>
    private IList<NLWebResult> EnhanceSearchResults(IList<NLWebResult> results, string originalQuery)
    {
        if (results == null || !results.Any())
            return results;

        // Sort results by relevance (simple implementation)
        var sortedResults = results
            .OrderByDescending(r => CalculateSearchRelevance(r, originalQuery))
            .ToList();

        // Add search-specific metadata
        foreach (var result in sortedResults)
        {
            if (string.IsNullOrEmpty(result.Site))
            {
                result.Site = "Search";
            }
        }

        return sortedResults;
    }

    /// <summary>
    /// Calculates search relevance score for a result.
    /// </summary>
    private double CalculateSearchRelevance(NLWebResult result, string query)
    {
        if (result == null || string.IsNullOrWhiteSpace(query))
            return result?.Score ?? 0.0;

        double score = result.Score;
        var queryLower = query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Name relevance (higher weight)
        if (!string.IsNullOrEmpty(result.Name))
        {
            var nameLower = result.Name.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (nameLower.Contains(term))
                    score += 3.0;
            }
        }

        // Description relevance (medium weight)
        if (!string.IsNullOrEmpty(result.Description))
        {
            var descriptionLower = result.Description.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (descriptionLower.Contains(term))
                    score += 2.0;
            }
        }

        return score;
    }

    /// <summary>
    /// Checks if the query contains explicit search keywords.
    /// </summary>
    private static bool ContainsSearchKeywords(string query)
    {
        var searchKeywords = new[] { "search", "find", "look for", "locate", "discover" };
        return searchKeywords.Any(keyword => query.Contains(keyword));
    }
}