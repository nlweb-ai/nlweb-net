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

            // Enhanced search processing - leverage existing QueryProcessor but with search-specific enhancements
            var enhancedRequest = await EnhanceSearchRequest(request, cancellationToken);
            
            // Use the existing query processor as the underlying engine
            var response = await QueryProcessor.ProcessQueryAsync(enhancedRequest, cancellationToken);
            
            // Post-process results for search-specific enhancements
            var enhancedResponse = await EnhanceSearchResponse(response, request, cancellationToken);
            
            stopwatch.Stop();
            enhancedResponse.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            enhancedResponse.Message = $"Enhanced search completed - found {enhancedResponse.Results.Count} results";
            
            Logger.LogDebug("Search tool completed in {ElapsedMs}ms with {ResultCount} results", 
                stopwatch.ElapsedMilliseconds, enhancedResponse.Results.Count);
            
            return enhancedResponse;
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
    /// Enhances the search request with search-specific optimizations.
    /// </summary>
    private async Task<NLWebRequest> EnhanceSearchRequest(NLWebRequest request, CancellationToken cancellationToken)
    {
        // Create enhanced request with search optimizations
        var enhancedRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = await OptimizeSearchQuery(request.Query, cancellationToken),
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = Math.Min(request.MaxResults ?? Options.MaxResultsPerQuery, Options.MaxResultsPerQuery),
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        Logger.LogDebug("Enhanced search query from '{Original}' to '{Enhanced}'", 
            request.Query, enhancedRequest.Query);

        return enhancedRequest;
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
    /// Enhances the search response with additional search-specific features.
    /// </summary>
    private Task<NLWebResponse> EnhanceSearchResponse(NLWebResponse response, NLWebRequest originalRequest, CancellationToken cancellationToken)
    {
        if (!response.Success || response.Results == null)
            return Task.FromResult(response);

        // Sort results by relevance (simple implementation)
        var sortedResults = response.Results
            .OrderByDescending(r => CalculateSearchRelevance(r, originalRequest.Query))
            .ToList();

        // Add search-specific metadata
        foreach (var result in sortedResults)
        {
            if (result is NLWebResult webResult && string.IsNullOrEmpty(webResult.Site))
            {
                webResult.Site = "Search";
            }
        }

        var enhancedResponse = new NLWebResponse
        {
            QueryId = response.QueryId,
            Query = response.Query,
            Mode = response.Mode,
            Results = sortedResults,
            Success = response.Success,
            Message = response.Message,
            ProcessingTimeMs = response.ProcessingTimeMs,
        };

        return Task.FromResult(enhancedResponse);
    }

    /// <summary>
    /// Calculates search relevance score for a result.
    /// </summary>
    private double CalculateSearchRelevance(NLWebResult result, string query)
    {
        if (result == null || string.IsNullOrWhiteSpace(query))
            return 0.0;

        double score = 0.0;
        var queryLower = query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Title relevance (higher weight)
        if (!string.IsNullOrEmpty(result.Name))
        {
            var titleLower = result.Name.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (titleLower.Contains(term))
                    score += 3.0;
            }
        }

        // Summary relevance (medium weight)
        if (!string.IsNullOrEmpty(result.Description))
        {
            var summaryLower = result.Description.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (summaryLower.Contains(term))
                    score += 2.0;
            }
        }

        // Content relevance (lower weight)
        {
            foreach (var term in queryTerms)
            {
                if (contentLower.Contains(term))
                    score += 1.0;
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