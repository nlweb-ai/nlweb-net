using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NLWebNet.Services;

/// <summary>
/// Tool handler for retrieving specific information about named items.
/// Focuses on providing detailed, comprehensive information about specific entities.
/// </summary>
public class DetailsToolHandler : BaseToolHandler
{
    public DetailsToolHandler(
        ILogger<DetailsToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    /// <inheritdoc />
    public override string ToolType => "details";

    /// <inheritdoc />
    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Executing details tool for query: {Query}", request.Query);

            // Extract the subject entity from the query
            var subject = ExtractSubject(request.Query);
            if (string.IsNullOrWhiteSpace(subject))
            {
                return CreateErrorResponse(request, "Could not identify the subject to get details about");
            }

            Logger.LogDebug("Extracted subject '{Subject}' from query", subject);

            // Create details-focused query
            var detailsQuery = $"{subject} overview definition explanation details";

            // Generate detailed results
            var searchResults = await ResultGenerator.GenerateListAsync(detailsQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();

            // Enhance results for details focus
            var detailsResults = EnhanceDetailsResults(resultsList, subject);

            stopwatch.Stop();

            var response = CreateSuccessResponse(request, detailsResults, stopwatch.ElapsedMilliseconds);
            response.ProcessedQuery = detailsQuery;
            response.Summary = $"Details retrieved for '{subject}' - found {detailsResults.Count} detailed results";

            Logger.LogDebug("Details tool completed in {ElapsedMs}ms for subject '{Subject}'",
                stopwatch.ElapsedMilliseconds, subject);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse(request, $"Details tool execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override bool CanHandle(NLWebRequest request)
    {
        if (!base.CanHandle(request))
            return false;

        var query = request.Query?.ToLowerInvariant() ?? string.Empty;

        // Can handle queries that ask for details, information, or descriptions
        var detailsKeywords = new[]
        {
            "details", "information about", "tell me about", "describe",
            "what is", "explain", "definition of", "overview of"
        };

        return detailsKeywords.Any(keyword => query.Contains(keyword));
    }

    /// <inheritdoc />
    public override int GetPriority(NLWebRequest request)
    {
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;

        // Higher priority for explicit details requests
        if (query.StartsWith("tell me about") || query.StartsWith("what is") || query.Contains("details about"))
            return 90;

        // Medium-high priority for informational queries
        if (query.Contains("information about") || query.Contains("describe"))
            return 75;

        // Default priority for details-related queries
        return 65;
    }

    /// <summary>
    /// Extracts the subject entity from a details query.
    /// </summary>
    private string ExtractSubject(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var queryLower = query.ToLowerInvariant().Trim();

        // Common patterns for details queries
        var patterns = new[]
        {
            @"(?:tell me about|information about|details about|describe)\s+(.+)",
            @"(?:what is|what are)\s+(.+)",
            @"(?:explain|definition of|overview of)\s+(.+)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(queryLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        // If no pattern matches, return the whole query
        return query;
    }

    /// <summary>
    /// Enhances results to focus on detailed information.
    /// </summary>
    private IList<NLWebResult> EnhanceDetailsResults(IList<NLWebResult> results, string subject)
    {
        if (results == null || !results.Any())
            return Array.Empty<NLWebResult>();

        // Filter and rank results by their detail relevance
        var detailResults = results
            .Select(r => new { Result = r, Score = CalculateDetailsRelevance(r, subject) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(10) // Focus on top detailed results
            .Select(x => x.Result)
            .ToList();

        // Enhance results with details-specific formatting
        foreach (var result in detailResults)
        {
            // Enhance name to indicate it's a details result
            if (!string.IsNullOrEmpty(result.Name) && !result.Name.ToLowerInvariant().Contains("details"))
            {
                result.Name = $"Details: {result.Name}";
            }

            // Set site to indicate details processing
            if (string.IsNullOrEmpty(result.Site))
            {
                result.Site = "Details";
            }
        }

        return detailResults;
    }

    /// <summary>
    /// Calculates how relevant a result is for providing details about the subject.
    /// </summary>
    private double CalculateDetailsRelevance(NLWebResult result, string subject)
    {
        if (result == null || string.IsNullOrWhiteSpace(subject))
            return result?.Score ?? 0.0;

        double score = result.Score;
        var subjectLower = subject.ToLowerInvariant();
        var subjectTerms = subjectLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check if result contains comprehensive information
        var detailsIndicators = new[] { "overview", "introduction", "definition", "explanation", "guide", "about" };

        // Name relevance with details indicators
        if (!string.IsNullOrEmpty(result.Name))
        {
            var nameLower = result.Name.ToLowerInvariant();

            // High score for exact subject match in name
            if (subjectTerms.All(term => nameLower.Contains(term)))
                score += 5.0;

            // Bonus for details indicators
            foreach (var indicator in detailsIndicators)
            {
                if (nameLower.Contains(indicator))
                    score += 2.0;
            }
        }

        // Description relevance
        if (!string.IsNullOrEmpty(result.Description))
        {
            var descriptionLower = result.Description.ToLowerInvariant();

            // Score for subject terms in description
            var matchingTerms = subjectTerms.Count(term => descriptionLower.Contains(term));
            score += matchingTerms * 1.5;

            // Bonus for comprehensive description (longer, more detailed)
            if (result.Description.Length > 100)
                score += 1.0;
        }

        return score;
    }
}