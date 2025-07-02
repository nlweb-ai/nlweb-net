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
        IQueryProcessor queryProcessor, IResultGenerator resultGenerator) 
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

            // Create details-focused request
            var detailsRequest = await CreateDetailsRequest(request, subject, cancellationToken);
            
            // Use the existing query processor to gather information
            var response = await QueryProcessor.ProcessQueryAsync(detailsRequest, cancellationToken);
            
            // Post-process results to focus on details
            var detailsResponse = await EnhanceDetailsResponse(response, subject, request, cancellationToken);
            
            stopwatch.Stop();
            detailsResponse.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            detailsResponse.Message = $"Details retrieved for '{subject}' - found {detailsResponse.Results.Count} detailed results";
            
            Logger.LogDebug("Details tool completed in {ElapsedMs}ms for subject '{Subject}'", 
                stopwatch.ElapsedMilliseconds, subject);
            
            return detailsResponse;
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
            @"(?:how does|how do)\s+(.+?)\s+(?:work|function)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(queryLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        // If no pattern matches, try to extract meaningful nouns (simple approach)
        var words = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stopWords = new HashSet<string> 
        { 
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "about"
        };

        var meaningfulWords = words.Where(w => !stopWords.Contains(w) && w.Length > 2).ToList();
        return meaningfulWords.Any() ? string.Join(" ", meaningfulWords) : query;
    }

    /// <summary>
    /// Creates a details-focused request for the specified subject.
    /// </summary>
    private Task<NLWebRequest> CreateDetailsRequest(NLWebRequest request, string subject, CancellationToken cancellationToken)
    {
        // Create a more specific query focused on getting comprehensive details
        var detailsQuery = $"{subject} overview definition explanation details";

        var detailsRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = detailsQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = Math.Min(request.MaxResults ?? 20, 20), // Limit results for focused details
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        Logger.LogDebug("Created details-focused query: {Query}", detailsQuery);
        return Task.FromResult(detailsRequest);
    }

    /// <summary>
    /// Enhances the response to focus on detailed information.
    /// </summary>
    private Task<NLWebResponse> EnhanceDetailsResponse(NLWebResponse response, string subject, NLWebRequest originalRequest, CancellationToken cancellationToken)
    {
        if (!response.Success || response.Results == null)
            return Task.FromResult(response);

        // Filter and rank results by their detail relevance
        var detailResults = response.Results
            .Select(r => new { Result = r, Score = CalculateDetailsRelevance(r, subject) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(15) // Focus on top detailed results
            .Select(x => x.Result)
            .ToList();

        // Enhance results with details-specific formatting
        foreach (var result in detailResults)
        {
            if (result is NLWebResult webResult)
            {
                // Enhance title to indicate it's a details result
                if (!string.IsNullOrEmpty(webResult.Name) && !webResult.Name.ToLowerInvariant().Contains("details"))
                {
                    webResult.Name = $"Details: {webResult.Name}";
                }

                // Set site to indicate details processing
                if (string.IsNullOrEmpty(webResult.Site))
                {
                    webResult.Site = "Details";
                }

                // Enhance summary to focus on key details
                if (!string.IsNullOrEmpty(webResult.Description))
                {
                    webResult.Description = EnhanceSummaryForDetails(webResult.Description, subject);
                }
            }
        }

        var enhancedResponse = new NLWebResponse
        {
            QueryId = response.QueryId,
            Query = response.Query,
            Mode = response.Mode,
            Results = detailResults,
            Success = response.Success,
            Message = response.Message,
            ProcessingTimeMs = response.ProcessingTimeMs,
        };

        return Task.FromResult(enhancedResponse);
    }

    /// <summary>
    /// Calculates how relevant a result is for providing details about the subject.
    /// </summary>
    private double CalculateDetailsRelevance(NLWebResult result, string subject)
    {
        if (result == null || string.IsNullOrWhiteSpace(subject))
            return 0.0;

        double score = 0.0;
        var subjectLower = subject.ToLowerInvariant();
        var subjectTerms = subjectLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check if result contains comprehensive information
        var detailsIndicators = new[] { "overview", "introduction", "definition", "explanation", "guide", "about" };
        
        // Title relevance with details indicators
        if (!string.IsNullOrEmpty(result.Name))
        {
            var titleLower = result.Name.ToLowerInvariant();
            
            // High score for exact subject match in title
            if (subjectTerms.All(term => titleLower.Contains(term)))
                score += 5.0;
            
            // Bonus for details indicators
            foreach (var indicator in detailsIndicators)
            {
                if (titleLower.Contains(indicator))
                    score += 2.0;
            }
        }

        // Summary relevance
        if (!string.IsNullOrEmpty(result.Description))
        {
            var summaryLower = result.Description.ToLowerInvariant();
            
            // Score for subject terms in summary
            var matchingTerms = subjectTerms.Count(term => summaryLower.Contains(term));
            score += matchingTerms * 1.5;
            
            // Bonus for comprehensive summary (longer, more detailed)
            if (result.Description.Length > 100)
                score += 1.0;
        }

        // Content depth bonus
        {
            score += 1.0;
        }

        return score;
    }

    /// <summary>
    /// Enhances a summary to better highlight details about the subject.
    /// </summary>
    private string EnhanceSummaryForDetails(string summary, string subject)
    {
        if (string.IsNullOrWhiteSpace(summary) || summary.Length < 50)
            return summary;

        // Simple enhancement - ensure summary starts with subject context if not already present
        var summaryLower = summary.ToLowerInvariant();
        var subjectLower = subject.ToLowerInvariant();

        if (!summaryLower.StartsWith(subjectLower) && summary.Length < 300)
        {
            return $"{subject}: {summary}";
        }

        return summary;
    }
}