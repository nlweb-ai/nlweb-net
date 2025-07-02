using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        IQueryProcessor queryProcessor, IResultGenerator resultGenerator) 
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

            // Analyze the recipe query type
            var recipeQuery = AnalyzeRecipeQuery(request.Query);
            if (recipeQuery.Type == RecipeQueryType.Unknown)
            {
                return CreateErrorResponse(request, "Could not identify the recipe-related request");
            }

            Logger.LogDebug("Identified recipe query type: {QueryType}", recipeQuery.Type);

            // Process based on query type
            var recipeResponse = await ProcessRecipeQuery(recipeQuery, request, cancellationToken);
            
            stopwatch.Stop();
            recipeResponse.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            recipeResponse.Message = $"Recipe query processed: {recipeQuery.Type}";
            
            Logger.LogDebug("Recipe tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return recipeResponse;
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
    /// Types of recipe queries the tool can handle.
    /// </summary>
    private enum RecipeQueryType
    {
        Unknown,
        Recipe,
        Substitution,
        Accompaniment,
        Technique,
        Nutrition
    }

    /// <summary>
    /// Parsed recipe query information.
    /// </summary>
    private class RecipeQuery
    {
        public RecipeQueryType Type { get; set; } = RecipeQueryType.Unknown;
        public string Subject { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = new();
    }

    /// <summary>
    /// Analyzes the query to determine the type of recipe request.
    /// </summary>
    private RecipeQuery AnalyzeRecipeQuery(string query)
    {
        var recipeQuery = new RecipeQuery();
        var queryLower = query.ToLowerInvariant().Trim();

        // Determine query type
        if (ContainsSubstitutionTerms(queryLower))
        {
            recipeQuery.Type = RecipeQueryType.Substitution;
            recipeQuery.Subject = ExtractSubstitutionSubject(queryLower);
        }
        else if (ContainsAccompanimentTerms(queryLower))
        {
            recipeQuery.Type = RecipeQueryType.Accompaniment;
            recipeQuery.Subject = ExtractAccompanimentSubject(queryLower);
        }
        else if (ContainsRecipeTerms(queryLower))
        {
            recipeQuery.Type = RecipeQueryType.Recipe;
            recipeQuery.Subject = ExtractRecipeSubject(queryLower);
        }
        else if (ContainsTechniqueTerms(queryLower))
        {
            recipeQuery.Type = RecipeQueryType.Technique;
            recipeQuery.Subject = ExtractTechniqueSubject(queryLower);
        }
        else if (ContainsNutritionTerms(queryLower))
        {
            recipeQuery.Type = RecipeQueryType.Nutrition;
            recipeQuery.Subject = ExtractNutritionSubject(queryLower);
        }

        // Extract ingredients if present
        recipeQuery.Ingredients = ExtractIngredients(queryLower);
        
        // Extract context
        recipeQuery.Context = ExtractContext(queryLower);

        return recipeQuery;
    }

    /// <summary>
    /// Processes the recipe query based on its type.
    /// </summary>
    private async Task<NLWebResponse> ProcessRecipeQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        return recipeQuery.Type switch
        {
            RecipeQueryType.Substitution => await ProcessSubstitutionQuery(recipeQuery, request, cancellationToken),
            RecipeQueryType.Accompaniment => await ProcessAccompanimentQuery(recipeQuery, request, cancellationToken),
            RecipeQueryType.Recipe => await ProcessRecipeSearchQuery(recipeQuery, request, cancellationToken),
            RecipeQueryType.Technique => await ProcessTechniqueQuery(recipeQuery, request, cancellationToken),
            RecipeQueryType.Nutrition => await ProcessNutritionQuery(recipeQuery, request, cancellationToken),
            _ => CreateErrorResponse(request, "Unknown recipe query type")
        };
    }

    /// <summary>
    /// Processes ingredient substitution queries.
    /// </summary>
    private async Task<NLWebResponse> ProcessSubstitutionQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        var substitutionQuery = $"substitute {recipeQuery.Subject} cooking ingredient alternative replacement";
        
        var substitutionRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = substitutionQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = 10,
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        var response = await QueryProcessor.ProcessQueryAsync(substitutionRequest, cancellationToken);
        
        if (response.Success && response.Results != null)
        {
            var substitutionResults = await EnhanceSubstitutionResults(response.Results, recipeQuery.Subject);
            return CreateSuccessResponse(request, substitutionResults, 0);
        }

        return CreateErrorResponse(request, "Could not find substitution information");
    }

    /// <summary>
    /// Processes accompaniment suggestion queries.
    /// </summary>
    private async Task<NLWebResponse> ProcessAccompanimentQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        var accompanimentQuery = $"what goes with {recipeQuery.Subject} side dish pairing accompaniment serve";
        
        var accompanimentRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = accompanimentQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = 10,
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        var response = await QueryProcessor.ProcessQueryAsync(accompanimentRequest, cancellationToken);
        
        if (response.Success && response.Results != null)
        {
            var accompanimentResults = await EnhanceAccompanimentResults(response.Results, recipeQuery.Subject);
            return CreateSuccessResponse(request, accompanimentResults, 0);
        }

        return CreateErrorResponse(request, "Could not find accompaniment suggestions");
    }

    /// <summary>
    /// Processes general recipe search queries.
    /// </summary>
    private async Task<NLWebResponse> ProcessRecipeSearchQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        var recipeSearchQuery = $"recipe {recipeQuery.Subject} cooking instructions preparation";
        
        var recipeRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = recipeSearchQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = 8,
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        var response = await QueryProcessor.ProcessQueryAsync(recipeRequest, cancellationToken);
        
        if (response.Success && response.Results != null)
        {
            var recipeResults = await EnhanceRecipeResults(response.Results, recipeQuery.Subject);
            return CreateSuccessResponse(request, recipeResults, 0);
        }

        return CreateErrorResponse(request, "Could not find recipe information");
    }

    /// <summary>
    /// Processes cooking technique queries.
    /// </summary>
    private async Task<NLWebResponse> ProcessTechniqueQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        var techniqueQuery = $"how to {recipeQuery.Subject} cooking technique method instructions";
        
        var techniqueRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = techniqueQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = 8,
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        var response = await QueryProcessor.ProcessQueryAsync(techniqueRequest, cancellationToken);
        
        if (response.Success && response.Results != null)
        {
            var techniqueResults = await EnhanceTechniqueResults(response.Results, recipeQuery.Subject);
            return CreateSuccessResponse(request, techniqueResults, 0);
        }

        return CreateErrorResponse(request, "Could not find technique information");
    }

    /// <summary>
    /// Processes nutrition-related queries.
    /// </summary>
    private async Task<NLWebResponse> ProcessNutritionQuery(RecipeQuery recipeQuery, NLWebRequest request, CancellationToken cancellationToken)
    {
        var nutritionQuery = $"{recipeQuery.Subject} nutrition facts calories vitamins minerals health";
        
        var nutritionRequest = new NLWebRequest
        {
            QueryId = request.QueryId,
            Query = nutritionQuery,
            Mode = request.Mode,
            Site = request.Site,
            MaxResults = 6,
            TimeoutSeconds = request.TimeoutSeconds,
            DecontextualizedQuery = request.DecontextualizedQuery,
            Context = request.Context
        };

        var response = await QueryProcessor.ProcessQueryAsync(nutritionRequest, cancellationToken);
        
        if (response.Success && response.Results != null)
        {
            var nutritionResults = await EnhanceNutritionResults(response.Results, recipeQuery.Subject);
            return CreateSuccessResponse(request, nutritionResults, 0);
        }

        return CreateErrorResponse(request, "Could not find nutrition information");
    }

    // Helper methods for detecting query types
    private bool ContainsSubstitutionTerms(string query) =>
        new[] { "substitute", "substitution", "replace", "instead of", "alternative to" }.Any(query.Contains);

    private bool ContainsAccompanimentTerms(string query) =>
        new[] { "goes with", "serve with", "pair with", "side dish", "accompaniment" }.Any(query.Contains);

    private bool ContainsRecipeTerms(string query) =>
        new[] { "recipe for", "how to make", "cook", "bake", "prepare" }.Any(query.Contains);

    private bool ContainsTechniqueTerms(string query) =>
        new[] { "how to", "technique", "method", "process", "way to" }.Any(query.Contains);

    private bool ContainsNutritionTerms(string query) =>
        new[] { "nutrition", "calories", "healthy", "vitamins", "nutrients" }.Any(query.Contains);

    // Helper methods for extracting subjects
    private string ExtractSubstitutionSubject(string query)
    {
        var patterns = new[]
        {
            @"substitute (?:for )?(.+?)(?:\s+with|\s+in|\s+when|$)",
            @"(?:replace|instead of|alternative to)\s+(.+?)(?:\s+with|\s+in|$)",
        };

        return ExtractWithPatterns(query, patterns);
    }

    private string ExtractAccompanimentSubject(string query)
    {
        var patterns = new[]
        {
            @"(?:goes with|serve with|pair with)\s+(.+?)(?:\s|$)",
            @"(.+?)\s+(?:goes with|pairs with|accompaniment)",
            @"side dish for\s+(.+?)(?:\s|$)",
        };

        return ExtractWithPatterns(query, patterns);
    }

    private string ExtractRecipeSubject(string query)
    {
        var patterns = new[]
        {
            @"recipe for\s+(.+?)(?:\s|$)",
            @"(?:how to make|cook|bake|prepare)\s+(.+?)(?:\s|$)",
        };

        return ExtractWithPatterns(query, patterns);
    }

    private string ExtractTechniqueSubject(string query)
    {
        var patterns = new[]
        {
            @"how to\s+(.+?)(?:\s|$)",
            @"(.+?)\s+technique",
            @"method (?:for|of)\s+(.+?)(?:\s|$)",
        };

        return ExtractWithPatterns(query, patterns);
    }

    private string ExtractNutritionSubject(string query)
    {
        var patterns = new[]
        {
            @"nutrition (?:of|in|for)\s+(.+?)(?:\s|$)",
            @"(.+?)\s+(?:nutrition|calories|healthy)",
        };

        return ExtractWithPatterns(query, patterns);
    }

    private string ExtractWithPatterns(string query, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        return string.Empty;
    }

    private List<string> ExtractIngredients(string query)
    {
        // Simple ingredient extraction - could be enhanced with NLP
        var ingredients = new List<string>();
        
        // Look for common ingredient patterns
        var ingredientPattern = @"\b(chicken|beef|pork|fish|salmon|rice|pasta|onion|garlic|tomato|cheese|milk|eggs?|flour|sugar|salt|pepper)\b";
        var matches = Regex.Matches(query, ingredientPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            if (!ingredients.Contains(match.Value.ToLowerInvariant()))
            {
                ingredients.Add(match.Value.ToLowerInvariant());
            }
        }
        
        return ingredients;
    }

    private string ExtractContext(string query)
    {
        var contextPatterns = new[]
        {
            @"for\s+(dinner|lunch|breakfast|dessert)",
            @"when\s+(cooking|baking|preparing)",
            @"in\s+(italian|mexican|chinese|french|indian)\s+cooking",
        };

        foreach (var pattern in contextPatterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value.Trim();
            }
        }

        return string.Empty;
    }

    // Result enhancement methods
    private Task<IList<NLWebResult>> EnhanceSubstitutionResults(IList<NLWebResult> results, string subject)
    {
        var enhancedResults = new List<NLWebResult>();
        
        // Add substitution overview
        enhancedResults.Add(new NLWebResult
        {
            Title = $"Substitutions for {subject}",
            Summary = $"Find suitable alternatives and substitutions for {subject} in your recipes",
            Url = string.Empty,
            Site = "Recipe",
            Content = "Ingredient substitution guide",
            Timestamp = DateTime.UtcNow
        });

        // Filter and enhance relevant results
        var relevantResults = results
            .Where(r => IsRelevantForSubstitution(r, subject))
            .Take(5)
            .ToList();

        foreach (var result in relevantResults)
        {
            if (result is NLWebResult webResult)
            {
                enhancedResults.Add(new NLWebResult
                {
                    Title = $"[Substitution] {webResult.Name}",
                    Summary = webResult.Description,
                    Url = webResult.Url,
                    Site = webResult.Site ?? "Recipe",
                });
            }
        }

        return Task.FromResult<IList<NLWebResult>>(enhancedResults);
    }

    private Task<IList<NLWebResult>> EnhanceAccompanimentResults(IList<NLWebResult> results, string subject)
    {
        var enhancedResults = new List<NLWebResult>();
        
        // Add accompaniment overview
        enhancedResults.Add(new NLWebResult
        {
            Title = $"What to Serve with {subject}",
            Summary = $"Discover perfect side dishes and accompaniments for {subject}",
            Url = string.Empty,
            Site = "Recipe",
            Content = "Food pairing suggestions",
            Timestamp = DateTime.UtcNow
        });

        // Filter and enhance relevant results
        var relevantResults = results
            .Where(r => IsRelevantForAccompaniment(r, subject))
            .Take(5)
            .ToList();

        foreach (var result in relevantResults)
        {
            if (result is NLWebResult webResult)
            {
                enhancedResults.Add(new NLWebResult
                {
                    Title = $"[Pairing] {webResult.Name}",
                    Summary = webResult.Description,
                    Url = webResult.Url,
                    Site = webResult.Site ?? "Recipe",
                });
            }
        }

        return Task.FromResult<IList<NLWebResult>>(enhancedResults);
    }

    private Task<IList<NLWebResult>> EnhanceRecipeResults(IList<NLWebResult> results, string subject)
    {
        var enhancedResults = results
            .Take(6)
            .Select(r => new NLWebResult
            {
                Title = $"[Recipe] {r.Name}",
                Summary = r.Description,
                Url = r.Url,
                Site = r.Site ?? "Recipe",
            })
            .Cast<NLWebResult>()
            .ToList();

        return Task.FromResult<IList<NLWebResult>>(enhancedResults);
    }

    private Task<IList<NLWebResult>> EnhanceTechniqueResults(IList<NLWebResult> results, string subject)
    {
        var enhancedResults = results
            .Take(5)
            .Select(r => new NLWebResult
            {
                Title = $"[Technique] {r.Name}",
                Summary = r.Description,
                Url = r.Url,
                Site = r.Site ?? "Recipe",
            })
            .Cast<NLWebResult>()
            .ToList();

        return Task.FromResult<IList<NLWebResult>>(enhancedResults);
    }

    private Task<IList<NLWebResult>> EnhanceNutritionResults(IList<NLWebResult> results, string subject)
    {
        var enhancedResults = results
            .Take(4)
            .Select(r => new NLWebResult
            {
                Title = $"[Nutrition] {r.Name}",
                Summary = r.Description,
                Url = r.Url,
                Site = r.Site ?? "Recipe",
            })
            .Cast<NLWebResult>()
            .ToList();

        return Task.FromResult<IList<NLWebResult>>(enhancedResults);
    }

    private bool IsRelevantForSubstitution(NLWebResult result, string subject)
    {
        var text = $"{result.Name} {result.Description}".ToLowerInvariant();
        return text.Contains("substitute") || text.Contains("alternative") || text.Contains("replace");
    }

    private bool IsRelevantForAccompaniment(NLWebResult result, string subject)
    {
        var text = $"{result.Name} {result.Description}".ToLowerInvariant();
        return text.Contains("side") || text.Contains("pair") || text.Contains("serve") || text.Contains("goes with");
    }
}