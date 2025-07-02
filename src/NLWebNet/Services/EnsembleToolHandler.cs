using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        IQueryProcessor queryProcessor, IResultGenerator resultGenerator) 
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

            // Analyze the query to understand the ensemble requirements
            var ensembleSpec = AnalyzeEnsembleRequirements(request.Query);
            if (ensembleSpec.Categories.Count == 0)
            {
                return CreateErrorResponse(request, "Could not identify ensemble requirements from query");
            }

            Logger.LogDebug("Identified ensemble with {CategoryCount} categories: {Categories}", 
                ensembleSpec.Categories.Count, string.Join(", ", ensembleSpec.Categories));

            // Gather items for each category
            var ensembleItems = new Dictionary<string, IList<NLWebResult>>();
            foreach (var category in ensembleSpec.Categories)
            {
                var categoryItems = await GatherCategoryItems(category, ensembleSpec.Theme, request, cancellationToken);
                ensembleItems[category] = categoryItems;
            }

            // Create cohesive ensemble response
            var ensembleResponse = CreateEnsembleResponse(request, ensembleSpec, ensembleItems);
            
            stopwatch.Stop();
            ensembleResponse.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            ensembleResponse.Message = $"Ensemble created with {ensembleSpec.Categories.Count} categories for theme '{ensembleSpec.Theme}'";
            
            Logger.LogDebug("Ensemble tool completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return ensembleResponse;
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

        // Also look for multiple categories being requested
        var categoryPatterns = new[]
        {
            @"(appetizer|starter).+(main|entree).+(dessert)",
            @"(museum|attraction).+(restaurant|food)",
            @"(hotel|accommodation).+(restaurant|dining)",
            @"(dinner|meal).+(entertainment|show|movie)",
            @"(breakfast|lunch|dinner).+(activity|entertainment)"
        };

        return ensembleKeywords.Any(keyword => query.Contains(keyword)) ||
               categoryPatterns.Any(pattern => Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase));
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
    /// Specification for an ensemble request.
    /// </summary>
    private class EnsembleSpecification
    {
        public string Theme { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public int ItemsPerCategory { get; set; } = 3;
    }

    /// <summary>
    /// Analyzes the query to understand ensemble requirements.
    /// </summary>
    private EnsembleSpecification AnalyzeEnsembleRequirements(string query)
    {
        var spec = new EnsembleSpecification();
        var queryLower = query.ToLowerInvariant();

        // Extract theme/context
        spec.Theme = ExtractTheme(queryLower);
        spec.Context = ExtractContext(queryLower);

        // Extract categories
        spec.Categories = ExtractCategories(queryLower);

        // Determine items per category based on query complexity
        if (spec.Categories.Count > 3)
            spec.ItemsPerCategory = 2;
        else if (spec.Categories.Count > 1)
            spec.ItemsPerCategory = 3;
        else
            spec.ItemsPerCategory = 5;

        return spec;
    }

    /// <summary>
    /// Extracts the main theme from the query.
    /// </summary>
    private string ExtractTheme(string query)
    {
        // Common theme patterns
        var themePatterns = new Dictionary<string, string[]>
        {
            ["italian"] = new[] { "italian", "italy" },
            ["romantic"] = new[] { "romantic", "date night", "anniversary" },
            ["family"] = new[] { "family", "kids", "children" },
            ["business"] = new[] { "business", "professional", "corporate" },
            ["casual"] = new[] { "casual", "relaxed", "informal" },
            ["fine dining"] = new[] { "fine dining", "upscale", "elegant" },
            ["seattle"] = new[] { "seattle", "washington", "pacific northwest" },
            ["tourist"] = new[] { "visiting", "tourist", "travel", "vacation" }
        };

        foreach (var theme in themePatterns)
        {
            if (theme.Value.Any(keyword => query.Contains(keyword)))
            {
                return theme.Key;
            }
        }

        // Try to extract theme from common patterns
        var match = Regex.Match(query, @"for (?:an?|the)?\s*(.+?)(?:\s+dinner|\s+meal|\s+day|\s+night|$)", RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        return "general";
    }

    /// <summary>
    /// Extracts context information from the query.
    /// </summary>
    private string ExtractContext(string query)
    {
        var contextIndicators = new[]
        {
            @"i'm visiting (.+?) for",
            @"in (.+?) for",
            @"around (.+?)(?:\s|$)",
            @"near (.+?)(?:\s|$)"
        };

        foreach (var pattern in contextIndicators)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts category requirements from the query.
    /// </summary>
    private List<string> ExtractCategories(string query)
    {
        var categories = new List<string>();

        // Food categories
        var foodCategories = new Dictionary<string, string[]>
        {
            ["appetizer"] = new[] { "appetizer", "starter", "appetiser" },
            ["main course"] = new[] { "main", "entree", "main course", "entrée" },
            ["dessert"] = new[] { "dessert", "sweet", "pudding" },
            ["drink"] = new[] { "drink", "beverage", "cocktail", "wine" }
        };

        // Activity categories  
        var activityCategories = new Dictionary<string, string[]>
        {
            ["museum"] = new[] { "museum", "gallery", "exhibition" },
            ["restaurant"] = new[] { "restaurant", "dining", "food", "eat" },
            ["entertainment"] = new[] { "entertainment", "show", "movie", "theater", "theatre" },
            ["attraction"] = new[] { "attraction", "sightseeing", "landmark", "tourist spot" },
            ["hotel"] = new[] { "hotel", "accommodation", "stay", "lodging" },
            ["activity"] = new[] { "activity", "things to do", "experience" }
        };

        // Check food categories
        foreach (var category in foodCategories)
        {
            if (category.Value.Any(keyword => query.Contains(keyword)))
            {
                categories.Add(category.Key);
            }
        }

        // Check activity categories
        foreach (var category in activityCategories)
        {
            if (category.Value.Any(keyword => query.Contains(keyword)))
            {
                categories.Add(category.Key);
            }
        }

        // If no specific categories found, try to infer from context
        if (categories.Count == 0)
        {
            if (query.Contains("dinner") || query.Contains("meal"))
            {
                categories.AddRange(new[] { "appetizer", "main course", "dessert" });
            }
            else if (query.Contains("day") || query.Contains("visit"))
            {
                categories.AddRange(new[] { "attraction", "restaurant" });
            }
            else if (query.Contains("recommend") || query.Contains("suggest"))
            {
                categories.Add("recommendation");
            }
        }

        return categories;
    }

    /// <summary>
    /// Gathers items for a specific category within the ensemble theme.
    /// </summary>
    private async Task<IList<NLWebResult>> GatherCategoryItems(string category, string theme, NLWebRequest originalRequest, CancellationToken cancellationToken)
    {
        // Create a focused query for this category and theme
        var categoryQuery = BuildCategoryQuery(category, theme, originalRequest.Query);
        
        var categoryRequest = new NLWebRequest
        {
            QueryId = originalRequest.QueryId,
            Query = categoryQuery,
            Mode = originalRequest.Mode,
            Site = originalRequest.Site,
            MaxResults = 8, // Get extra to filter best
            TimeoutSeconds = originalRequest.TimeoutSeconds,
            DecontextualizedQuery = originalRequest.DecontextualizedQuery,
            Context = originalRequest.Context
        };

        try
        {
            Logger.LogDebug("Gathering {Category} items with query: {Query}", category, categoryQuery);
            
            var response = await QueryProcessor.ProcessQueryAsync(categoryRequest, cancellationToken);
            
            if (response.Success && response.Results != null)
            {
                // Filter and rank results for this category
                return response.Results
                    .Where(r => IsRelevantForCategory(r, category, theme))
                    .Take(3) // Limit to top results
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to gather {Category} items", category);
        }

        return new List<NLWebResult>();
    }

    /// <summary>
    /// Builds a category-specific query.
    /// </summary>
    private string BuildCategoryQuery(string category, string theme, string originalQuery)
    {
        var themeContext = string.IsNullOrEmpty(theme) || theme == "general" ? "" : $" {theme}";
        
        return category switch
        {
            "appetizer" => $"best{themeContext} appetizers starters",
            "main course" => $"best{themeContext} main course entrees",
            "dessert" => $"best{themeContext} desserts",
            "drink" => $"best{themeContext} drinks beverages",
            "museum" => $"best museums{themeContext} attractions",
            "restaurant" => $"best restaurants{themeContext} dining",
            "entertainment" => $"best entertainment{themeContext} shows",
            "attraction" => $"best attractions{themeContext} sightseeing",
            "hotel" => $"best hotels{themeContext} accommodation",
            "activity" => $"best activities{themeContext} things to do",
            _ => $"best {category}{themeContext}"
        };
    }

    /// <summary>
    /// Checks if a result is relevant for the given category and theme.
    /// </summary>
    private bool IsRelevantForCategory(NLWebResult result, string category, string theme)
    {
        if (result == null)
            return false;

        var text = $"{result.Name} {result.Description}".ToLowerInvariant();
        
        // Check for category relevance
        var categoryKeywords = GetCategoryKeywords(category);
        var hasCategoryMatch = categoryKeywords.Any(keyword => text.Contains(keyword));
        
        return hasCategoryMatch;
    }

    /// <summary>
    /// Gets keywords associated with a category.
    /// </summary>
    private string[] GetCategoryKeywords(string category)
    {
        return category switch
        {
            "appetizer" => new[] { "appetizer", "starter", "appetiser", "small plate" },
            "main course" => new[] { "main", "entree", "entrée", "dinner", "lunch" },
            "dessert" => new[] { "dessert", "sweet", "cake", "ice cream", "pudding" },
            "drink" => new[] { "drink", "beverage", "cocktail", "wine", "beer" },
            "museum" => new[] { "museum", "gallery", "exhibition", "art" },
            "restaurant" => new[] { "restaurant", "dining", "food", "cuisine" },
            "entertainment" => new[] { "show", "movie", "theater", "theatre", "entertainment" },
            "attraction" => new[] { "attraction", "landmark", "sightseeing", "visit" },
            "hotel" => new[] { "hotel", "accommodation", "stay", "lodging" },
            "activity" => new[] { "activity", "experience", "tour", "adventure" },
            _ => new[] { category }
        };
    }

    /// <summary>
    /// Creates a cohesive ensemble response from gathered items.
    /// </summary>
    private NLWebResponse CreateEnsembleResponse(NLWebRequest request, EnsembleSpecification spec, Dictionary<string, IList<NLWebResult>> ensembleItems)
    {
        var ensembleResults = new List<NLWebResult>();

        // Create ensemble overview
        var overview = CreateEnsembleOverview(spec, ensembleItems);
        ensembleResults.Add(overview);

        // Add results for each category
        foreach (var category in spec.Categories)
        {
            if (ensembleItems.TryGetValue(category, out var categoryItems) && categoryItems.Any())
            {
                // Add category header
                var categoryHeader = new NLWebResult
                {
                    Title = $"{char.ToUpper(category[0])}{category.Substring(1)} Options",
                    Summary = $"Recommended {category} options for your {spec.Theme} ensemble",
                    Url = string.Empty,
                    Site = "Ensemble",
                    Content = string.Empty,
                    Timestamp = DateTime.UtcNow
                };
                ensembleResults.Add(categoryHeader);

                // Add category items with ensemble context
                foreach (var item in categoryItems)
                {
                    if (item is NLWebResult webResult)
                    {
                        var ensembleItem = new NLWebResult
                        {
                            Title = $"[{category}] {webResult.Name}",
                            Summary = webResult.Description,
                            Url = webResult.Url,
                            Site = webResult.Site ?? "Ensemble",
                        };
                        ensembleResults.Add(ensembleItem);
                    }
                }
            }
        }

        return CreateSuccessResponse(request, ensembleResults, 0);
    }

    /// <summary>
    /// Creates an overview of the ensemble.
    /// </summary>
    private NLWebResult CreateEnsembleOverview(EnsembleSpecification spec, Dictionary<string, IList<NLWebResult>> ensembleItems)
    {
        var overview = $"**{char.ToUpper(spec.Theme[0])}{spec.Theme.Substring(1)} Ensemble**\n\n";
        
        if (!string.IsNullOrEmpty(spec.Context))
        {
            overview += $"*Context: {spec.Context}*\n\n";
        }

        overview += "This curated ensemble includes:\n\n";
        
        foreach (var category in spec.Categories)
        {
            if (ensembleItems.TryGetValue(category, out var items) && items.Any())
            {
                overview += $"• **{char.ToUpper(category[0])}{category.Substring(1)}**: {items.Count} options\n";
            }
            else
            {
                overview += $"• **{char.ToUpper(category[0])}{category.Substring(1)}**: No specific recommendations found\n";
            }
        }

        overview += "\nEach category has been carefully selected to complement the others in this ensemble.";

        return new NLWebResult
        {
            Title = $"Curated {spec.Theme} Ensemble",
            Summary = overview,
            Url = string.Empty,
            Site = "Ensemble",
            Content = "Comprehensive ensemble planning",
            Timestamp = DateTime.UtcNow
        };
    }
}