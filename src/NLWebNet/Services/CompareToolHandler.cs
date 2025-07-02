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
        IQueryProcessor queryProcessor, IResultGenerator resultGenerator) 
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

            // Gather information about both items
            var item1Results = await GatherItemInformation(comparisonItems.Item1, request, cancellationToken);
            var item2Results = await GatherItemInformation(comparisonItems.Item2, request, cancellationToken);
            
            // Create structured comparison results
            var comparisonResponse = CreateComparisonResponse(
                request, comparisonItems.Item1, comparisonItems.Item2, 
                item1Results, item2Results);
            
            stopwatch.Stop();
            comparisonResponse.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            comparisonResponse.Message = $"Comparison completed between '{comparisonItems.Item1}' and '{comparisonItems.Item2}'";
            
            Logger.LogDebug("Compare tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return comparisonResponse;
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
            // "A or B" (when asking which is better)
            @"(.+?)\s+or\s+(.+?)(?:\s+which|$)",
            // "A and B comparison"
            @"(.+?)\s+and\s+(.+?)\s+comparison",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(queryLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 2)
            {
                var item1 = CleanComparisonItem(match.Groups[1].Value);
                var item2 = CleanComparisonItem(match.Groups[2].Value);
                
                if (!string.IsNullOrWhiteSpace(item1) && !string.IsNullOrWhiteSpace(item2))
                {
                    return (item1, item2);
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Cleans up extracted comparison items by removing noise words.
    /// </summary>
    private string CleanComparisonItem(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
            return string.Empty;

        var cleaned = item.Trim();
        
        // Remove common noise words from the beginning
        var prefixNoise = new[] { "the", "a", "an", "which", "what", "how" };
        foreach (var noise in prefixNoise)
        {
            if (cleaned.StartsWith(noise + " ", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(noise.Length + 1).Trim();
            }
        }

        // Remove common noise words from the end
        var suffixNoise = new[] { "better", "worse", "best", "good", "bad" };
        foreach (var noise in suffixNoise)
        {
            if (cleaned.EndsWith(" " + noise, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - noise.Length - 1).Trim();
            }
        }

        return cleaned;
    }

    /// <summary>
    /// Gathers information about a specific item for comparison.
    /// </summary>
    private async Task<IList<NLWebResult>> GatherItemInformation(string item, NLWebRequest originalRequest, CancellationToken cancellationToken)
    {
        // Create a focused query for this specific item
        var itemQuery = $"{item} features overview specifications";
        
        var itemRequest = new NLWebRequest
        {
            QueryId = originalRequest.QueryId,
            Query = itemQuery,
            Mode = originalRequest.Mode,
            Site = originalRequest.Site,
            MaxResults = 5, // Limit results per item
            TimeoutSeconds = originalRequest.TimeoutSeconds,
            DecontextualizedQuery = originalRequest.DecontextualizedQuery,
            Context = originalRequest.Context
        };

        try
        {
            var response = await QueryProcessor.ProcessQueryAsync(itemRequest, cancellationToken);
            return response.Success ? response.Results : new List<NLWebResult>();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to gather information for item '{Item}'", item);
            return new List<NLWebResult>();
        }
    }

    /// <summary>
    /// Creates a structured comparison response from the gathered information.
    /// </summary>
    private NLWebResponse CreateComparisonResponse(
        NLWebRequest request, 
        string item1, 
        string item2, 
        IList<NLWebResult> item1Results, 
        IList<NLWebResult> item2Results)
    {
        var comparisonResults = new List<NLWebResult>();

        // Create summary comparison result
        var summaryResult = CreateComparisonSummary(item1, item2, item1Results, item2Results);
        comparisonResults.Add(summaryResult);

        // Add detailed results for item 1
        var item1Section = CreateItemSection(item1, item1Results, "A");
        comparisonResults.AddRange(item1Section);

        // Add detailed results for item 2
        var item2Section = CreateItemSection(item2, item2Results, "B");
        comparisonResults.AddRange(item2Section);

        // Add side-by-side comparison if we have good data
        if (item1Results.Any() && item2Results.Any())
        {
            var sideBySideResult = CreateSideBySideComparison(item1, item2, item1Results, item2Results);
            comparisonResults.Add(sideBySideResult);
        }

        return CreateSuccessResponse(request, comparisonResults, 0);
    }

    /// <summary>
    /// Creates a high-level comparison summary.
    /// </summary>
    private NLWebResult CreateComparisonSummary(string item1, string item2, IList<NLWebResult> item1Results, IList<NLWebResult> item2Results)
    {
        var summary = $"Comparison between {item1} and {item2}:\n\n";
        
        if (item1Results.Any())
        {
            summary += $"**{item1}**: {GetBestSummary(item1Results)}\n\n";
        }
        
        if (item2Results.Any())
        {
            summary += $"**{item2}**: {GetBestSummary(item2Results)}\n\n";
        }

        if (!item1Results.Any() && !item2Results.Any())
        {
            summary += "Limited information available for detailed comparison.";
        }

        return new NLWebResult
        {
            Title = $"Comparison: {item1} vs {item2}",
            Summary = summary,
            Url = string.Empty,
            Site = "Compare",
            Content = "Structured comparison analysis",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a section of results for a specific item.
    /// </summary>
    private IList<NLWebResult> CreateItemSection(string item, IList<NLWebResult> results, string section)
    {
        var sectionResults = new List<NLWebResult>();

        // Add section header
        sectionResults.Add(new NLWebResult
        {
            Title = $"Option {section}: {item}",
            Summary = $"Information about {item}",
            Url = string.Empty,
            Site = "Compare",
            Content = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        // Add the best results for this item
        var bestResults = results.Take(3).ToList();
        foreach (var result in bestResults)
        {
            if (result is NLWebResult webResult)
            {
                var enhancedResult = new NLWebResult
                {
                    Title = $"{item}: {webResult.Name}",
                    Summary = webResult.Description,
                    Url = webResult.Url,
                    Site = webResult.Site ?? "Compare",
                };
                sectionResults.Add(enhancedResult);
            }
        }

        return sectionResults;
    }

    /// <summary>
    /// Creates a side-by-side comparison result.
    /// </summary>
    private NLWebResult CreateSideBySideComparison(string item1, string item2, IList<NLWebResult> item1Results, IList<NLWebResult> item2Results)
    {
        var comparison = $"**Side-by-Side Comparison**\n\n";
        comparison += $"| Aspect | {item1} | {item2} |\n";
        comparison += "|--------|---------|----------|\n";
        
        // Extract key aspects from both sets of results
        var item1Summary = GetBestSummary(item1Results);
        var item2Summary = GetBestSummary(item2Results);
        
        comparison += $"| Overview | {TruncateForTable(item1Summary)} | {TruncateForTable(item2Summary)} |\n";
        
        return new NLWebResult
        {
            Title = $"Side-by-Side: {item1} vs {item2}",
            Summary = comparison,
            Url = string.Empty,
            Site = "Compare",
            Content = "Detailed side-by-side comparison table",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets the best summary from a collection of results.
    /// </summary>
    private string GetBestSummary(IList<NLWebResult> results)
    {
        var bestResult = results
            .Where(r => !string.IsNullOrWhiteSpace(r.Description))
            .OrderByDescending(r => r.Description?.Length ?? 0)
            .FirstOrDefault();

        return bestResult?.Description ?? "No detailed information available";
    }

    /// <summary>
    /// Truncates text for table display.
    /// </summary>
    private string TruncateForTable(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "N/A";

        const int maxLength = 100;
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}