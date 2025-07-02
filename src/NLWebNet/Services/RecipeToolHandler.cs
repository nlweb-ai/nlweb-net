using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;

namespace NLWebNet.Services;

/// <summary>
/// Tool handler for recipe-related queries including ingredient substitutions and accompaniment suggestions.
/// Handles cooking, recipe, and food-related queries with specialized knowledge.
/// </summary>
public class RecipeToolHandler : BaseToolHandler
{
    public RecipeToolHandler(
        ILogger<RecipeToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator) 
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    /// <inheritdoc />
    public override string ToolType => "recipe";

    /// <inheritdoc />
    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogDebug("Executing recipe tool for query: {Query}", request.Query);

            // Create recipe-focused query
            var recipeQuery = $"{request.Query} recipe cooking instructions";
            
            // Generate recipe results
            var searchResults = await ResultGenerator.GenerateListAsync(recipeQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();
            
            // Create recipe-specific results
            var recipeResults = CreateRecipeResults(resultsList, request.Query);
            
            stopwatch.Stop();
            
            var response = CreateSuccessResponse(request, recipeResults, stopwatch.ElapsedMilliseconds);
            response.ProcessedQuery = recipeQuery;
            response.Summary = $"Recipe information and cooking guidance provided";
            
            Logger.LogDebug("Recipe tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse(request, $"Recipe tool execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override bool CanHandle(NLWebRequest request)
    {
        if (!base.CanHandle(request))
            return false;

        var query = request.Query?.ToLowerInvariant() ?? string.Empty;
        
        // Can handle recipe, cooking, and food-related queries
        var recipeKeywords = new[] 
        { 
            "recipe", "cooking", "cook", "ingredient", "substitute", "substitution",
            "bake", "baking", "preparation", "kitchen", "culinary", "food",
            "accompaniment", "side dish", "pair with", "serve with", "goes with"
        };

        return recipeKeywords.Any(keyword => query.Contains(keyword));
    }

    /// <inheritdoc />
    public override int GetPriority(NLWebRequest request)
    {
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;
        
        // Higher priority for specific recipe operations
        if (query.Contains("substitute") || query.Contains("substitution"))
            return 95;
        
        if (query.Contains("recipe for") || query.Contains("how to cook"))
            return 90;
        
        if (query.Contains("serve with") || query.Contains("goes with"))
            return 85;
        
        // Medium priority for general cooking queries
        return 70;
    }

    /// <summary>
    /// Creates recipe-specific results from search results.
    /// </summary>
    private IList<NLWebResult> CreateRecipeResults(IList<NLWebResult> results, string originalQuery)
    {
        var recipeResults = new List<NLWebResult>();

        // Determine query type and create appropriate header
        var queryType = DetermineQueryType(originalQuery);
        recipeResults.Add(CreateToolResult(
            $"Recipe Guide: {queryType}",
            $"Culinary information and guidance for: {originalQuery}",
            "",
            "Recipe",
            1.0
        ));

        // Add recipe-specific results
        var relevantResults = results
            .Where(r => IsRelevantForRecipe(r, originalQuery))
            .Take(8)
            .Select(result => CreateToolResult(
                $"[{queryType}] {result.Name}",
                result.Description,
                result.Url,
                result.Site ?? "Recipe",
                result.Score
            ))
            .ToList();

        recipeResults.AddRange(relevantResults);
        return recipeResults;
    }

    /// <summary>
    /// Determines the type of recipe query.
    /// </summary>
    private string DetermineQueryType(string query)
    {
        var queryLower = query.ToLowerInvariant();
        
        if (queryLower.Contains("substitute"))
            return "Substitution";
        if (queryLower.Contains("serve with") || queryLower.Contains("goes with"))
            return "Pairing";
        if (queryLower.Contains("recipe"))
            return "Recipe";
        if (queryLower.Contains("cook") || queryLower.Contains("bake"))
            return "Technique";
        
        return "Cooking";
    }

    /// <summary>
    /// Checks if a result is relevant for recipe queries.
    /// </summary>
    private bool IsRelevantForRecipe(NLWebResult result, string originalQuery)
    {
        var text = $"{result.Name} {result.Description}".ToLowerInvariant();
        var recipeTerms = new[] { "recipe", "cooking", "food", "ingredient", "kitchen", "culinary" };
        
        return recipeTerms.Any(term => text.Contains(term));
    }
}