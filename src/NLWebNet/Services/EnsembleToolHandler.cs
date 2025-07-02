using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;

namespace NLWebNet.Services;

/// <summary>
/// Tool handler for creating cohesive sets of related items.
/// Handles queries like "Give me an appetizer, main and dessert for an Italian dinner" 
/// or "I'm visiting Seattle for a day - suggest museums and nearby restaurants".
/// </summary>
public class EnsembleToolHandler : BaseToolHandler
{
    public EnsembleToolHandler(
        ILogger<EnsembleToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator) 
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    /// <inheritdoc />
    public override string ToolType => "ensemble";

    /// <inheritdoc />
    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogDebug("Executing ensemble tool for query: {Query}", request.Query);

            // Create ensemble-focused query
            var ensembleQuery = $"{request.Query} recommendations suggestions set";
            
            // Generate ensemble results
            var searchResults = await ResultGenerator.GenerateListAsync(ensembleQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();
            
            // Create ensemble response
            var ensembleResults = CreateEnsembleResults(resultsList, request.Query);
            
            stopwatch.Stop();
            
            var response = CreateSuccessResponse(request, ensembleResults, stopwatch.ElapsedMilliseconds);
            response.ProcessedQuery = ensembleQuery;
            response.Summary = $"Ensemble recommendations created for your request";
            
            Logger.LogDebug("Ensemble tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse(request, $"Ensemble tool execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override bool CanHandle(NLWebRequest request)
    {
        if (!base.CanHandle(request))
            return false;

        var query = request.Query?.ToLowerInvariant() ?? string.Empty;
        
        // Can handle queries that ask for recommendations, suggestions, or sets of items
        var ensembleKeywords = new[] 
        { 
            "recommend", "suggest", "give me", "plan", "set of", "ensemble",
            "what should i", "help me choose", "i need", "looking for"
        };

        return ensembleKeywords.Any(keyword => query.Contains(keyword));
    }

    /// <inheritdoc />
    public override int GetPriority(NLWebRequest request)
    {
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;
        
        // Higher priority for explicit ensemble requests
        if (query.Contains("give me") && (query.Contains(" and ") || query.Contains(", ")))
            return 90;
        
        // High priority for planning requests
        if (query.StartsWith("plan") || query.Contains("help me plan"))
            return 85;
        
        // Medium-high priority for recommendation requests with multiple items
        if ((query.Contains("recommend") || query.Contains("suggest")) && 
            (query.Contains(" and ") || query.Contains(", ")))
            return 80;
        
        // Default priority for ensemble-related queries
        return 65;
    }

    /// <summary>
    /// Creates ensemble results from search results.
    /// </summary>
    private IList<NLWebResult> CreateEnsembleResults(IList<NLWebResult> results, string originalQuery)
    {
        var ensembleResults = new List<NLWebResult>();

        // Create ensemble overview
        ensembleResults.Add(CreateToolResult(
            "Curated Ensemble Recommendations",
            $"A carefully selected collection of recommendations based on your request: {originalQuery}",
            "",
            "Ensemble",
            1.0
        ));

        // Add categorized results
        var categorizedResults = results
            .Take(10)
            .Select((result, index) => CreateToolResult(
                $"[Option {index + 1}] {result.Name}",
                result.Description,
                result.Url,
                result.Site ?? "Ensemble",
                result.Score
            ))
            .ToList();

        ensembleResults.AddRange(categorizedResults);
        return ensembleResults;
    }
}