using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NLWebNet.Services;

/// <summary>
/// Tool handler for side-by-side comparison of two items.
/// Analyzes queries to identify comparison subjects and provides structured comparison results.
/// </summary>
public class CompareToolHandler : BaseToolHandler
{
    public CompareToolHandler(
        ILogger<CompareToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    /// <inheritdoc />
    public override string ToolType => "compare";

    /// <inheritdoc />
    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Executing compare tool for query: {Query}", request.Query);

            // Extract the items to compare from the query
            var comparisonItems = ExtractComparisonItems(request.Query);
            if (comparisonItems.Item1 == null || comparisonItems.Item2 == null)
            {
                return CreateErrorResponse(request, "Could not identify two items to compare");
            }

            Logger.LogDebug("Comparing '{Item1}' vs '{Item2}'", comparisonItems.Item1, comparisonItems.Item2);

            // Create comparison query
            var comparisonQuery = $"{comparisonItems.Item1} vs {comparisonItems.Item2} comparison differences";

            // Generate comparison results
            var searchResults = await ResultGenerator.GenerateListAsync(comparisonQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();

            // Create structured comparison results
            var comparisonResults = CreateComparisonResults(resultsList, comparisonItems.Item1, comparisonItems.Item2);

            stopwatch.Stop();

            var response = CreateSuccessResponse(request, comparisonResults, stopwatch.ElapsedMilliseconds);
            response.ProcessedQuery = comparisonQuery;
            response.Summary = $"Comparison completed between '{comparisonItems.Item1}' and '{comparisonItems.Item2}'";

            Logger.LogDebug("Compare tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse(request, $"Compare tool execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override bool CanHandle(NLWebRequest request)
    {
        if (!base.CanHandle(request))
            return false;

        var query = request.Query?.ToLowerInvariant() ?? string.Empty;

        // Can handle queries that contain comparison keywords
        var compareKeywords = new[]
        {
            "compare", "vs", "versus", "difference", "differences",
            "contrast", "better", "worse", "pros and cons", "which is better"
        };

        return compareKeywords.Any(keyword => query.Contains(keyword));
    }

    /// <inheritdoc />
    public override int GetPriority(NLWebRequest request)
    {
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;

        // Higher priority for explicit comparison requests
        if (query.Contains(" vs ") || query.Contains(" versus ") || query.StartsWith("compare"))
            return 95;

        // High priority for difference queries
        if (query.Contains("difference") || query.Contains("contrast"))
            return 85;

        // Medium priority for other comparison-related queries
        return 70;
    }

    /// <summary>
    /// Extracts the two items to compare from the query.
    /// </summary>
    private (string? Item1, string? Item2) ExtractComparisonItems(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (null, null);

        var queryLower = query.ToLowerInvariant().Trim();

        // Common comparison patterns
        var patterns = new[]
        {
            // "compare A vs B" or "compare A versus B"
            @"compare\s+(.+?)\s+(?:vs|versus)\s+(.+)",
            // "A vs B" or "A versus B"
            @"(.+?)\s+(?:vs|versus)\s+(.+)",
            // "difference between A and B"
            @"difference\s+between\s+(.+?)\s+and\s+(.+)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(queryLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 2)
            {
                var item1 = match.Groups[1].Value.Trim();
                var item2 = match.Groups[2].Value.Trim();

                if (!string.IsNullOrWhiteSpace(item1) && !string.IsNullOrWhiteSpace(item2))
                {
                    return (item1, item2);
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Creates structured comparison results.
    /// </summary>
    private IList<NLWebResult> CreateComparisonResults(IList<NLWebResult> results, string item1, string item2)
    {
        var comparisonResults = new List<NLWebResult>();

        // Create summary comparison result
        comparisonResults.Add(CreateToolResult(
            $"Comparison: {item1} vs {item2}",
            $"Side-by-side comparison analysis of {item1} and {item2}",
            "",
            "Compare",
            1.0
        ));

        // Add relevant comparison results
        var relevantResults = results
            .Where(r => IsRelevantForComparison(r, item1, item2))
            .Take(8)
            .ToList();

        foreach (var result in relevantResults)
        {
            comparisonResults.Add(CreateToolResult(
                $"[Compare] {result.Name}",
                result.Description,
                result.Url,
                result.Site ?? "Compare",
                result.Score
            ));
        }

        return comparisonResults;
    }

    /// <summary>
    /// Checks if a result is relevant for comparison.
    /// </summary>
    private bool IsRelevantForComparison(NLWebResult result, string item1, string item2)
    {
        var text = $"{result.Name} {result.Description}".ToLowerInvariant();
        return text.Contains(item1.ToLowerInvariant()) || text.Contains(item2.ToLowerInvariant()) ||
               text.Contains("compare") || text.Contains("difference") || text.Contains("versus");
    }
}