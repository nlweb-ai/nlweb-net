using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Services;

[TestClass]
public class RecipeToolHandlerTests
{
    private RecipeToolHandler _recipeToolHandler = null!;
    private TestLogger<RecipeToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<RecipeToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _recipeToolHandler = new RecipeToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void ToolType_ReturnsCorrectType()
    {
        // Act
        var toolType = _recipeToolHandler.ToolType;

        // Assert
        Assert.AreEqual("recipe", toolType);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBasicRecipeQuery_ReturnsRecipeResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recipe for chocolate cake",
            Mode = QueryMode.List,
            QueryId = "test-recipe-001"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Chocolate Cake Recipe", Description = "Easy chocolate cake recipe", Score = 0.9 },
            new NLWebResult { Name = "Baking Tips", Description = "Chocolate cake baking guide", Score = 0.8 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.IsGreaterThan(response.Results.Count, 0);
        var hasMatchingItem = response.Results.Any(r => r.Name.Contains("Recipe Guide"));
        Assert.IsTrue(hasMatchingItem);
        Assert.IsNotNull(response.ProcessingTimeMs);
        Assert.IsGreaterThanOrEqualTo(response.ProcessingTimeMs.Value, 0);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSubstitutionQuery_ReturnsSubstitutionResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "substitute for eggs in baking",
            Mode = QueryMode.List,
            QueryId = "test-recipe-002"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Egg Substitutes", Description = "Alternatives to eggs in baking", Score = 0.95 },
            new NLWebResult { Name = "Baking Without Eggs", Description = "Egg-free baking guide", Score = 0.85 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        var hasMatchingItem = response.Results.Any(r => r.Name.Contains("Substitution"));
        Assert.IsTrue(hasMatchingItem);
        Assert.Contains("Recipe information", response.Summary);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCookingQuery_ReturnsEnhancedResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "how to cook risotto",
            Mode = QueryMode.List,
            QueryId = "test-recipe-003"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Risotto Recipe", Description = "Traditional Italian risotto", Score = 0.9 },
            new NLWebResult { Name = "Cooking Techniques", Description = "Risotto cooking methods", Score = 0.8 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.IsGreaterThan(response.Results.Count, 0);
        Assert.Contains("recipe cooking instructions", response.ProcessedQuery);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithIngredientQuery_ProcessesCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what to do with leftover ingredients",
            Mode = QueryMode.List,
            QueryId = "test-recipe-004"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Leftover Recipes", Description = "Creative uses for leftover ingredients", Score = 0.85 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.IsGreaterThan(response.Results.Count, 0);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithPairingQuery_ReturnsAccompanimentResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what goes with grilled salmon",
            Mode = QueryMode.List,
            QueryId = "test-recipe-005"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Salmon Pairings", Description = "Best sides for salmon", Score = 0.9 },
            new NLWebResult { Name = "Wine Pairings", Description = "Wines that complement salmon", Score = 0.8 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        var hasMatchingItem = response.Results.Any(r => r.Name.Contains("Pairing"));
        Assert.IsTrue(hasMatchingItem);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBakingQuery_ReturnsSpecializedResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "baking bread techniques",
            Mode = QueryMode.List,
            QueryId = "test-recipe-006"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Bread Baking Guide", Description = "Professional bread baking techniques", Score = 0.95 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.IsNotEmpty(response.Results); // Just verify we have results
        Assert.Contains("recipe", response.ProcessedQuery); // Verify recipe query processing
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyResults_ReturnsEmptyRecipeList()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "obscure recipe query",
            Mode = QueryMode.List,
            QueryId = "test-recipe-007"
        };

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.HasCount(1, response.Results); // Only the recipe guide header
        Assert.IsTrue(response.Results[0].Name.Contains("Recipe Guide"));
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recipe for pasta",
            Mode = QueryMode.List,
            QueryId = "test-recipe-008"
        };

        // Setup result generator to throw cancellation exception
        _resultGenerator.ShouldThrowCancellation = true;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request, cts.Token);

        // Assert - tool should handle cancellation gracefully and return error response
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.IsTrue(response.Error.Contains("canceled") || response.Error.Contains("cancelled"));
    }

    [TestMethod]
    public void CanHandle_WithRecipeKeywords_ReturnsTrue()
    {
        // Arrange
        var recipeQueries = new[]
        {
            "recipe for chicken",
            "cooking pasta",
            "how to bake bread",
            "ingredient substitution",
            "culinary techniques",
            "food preparation",
            "kitchen tips",
            "serve with steak"
        };

        // Act & Assert
        foreach (var query in recipeQueries)
        {
            var request = new NLWebRequest { Query = query };
            var canHandle = _recipeToolHandler.CanHandle(request);
            Assert.IsTrue(canHandle, $"Should handle recipe query: {query}");
        }
    }

    [TestMethod]
    public void CanHandle_WithNonRecipeQueries_ReturnsFalse()
    {
        // Arrange
        var nonRecipeQueries = new[]
        {
            "weather forecast",
            "stock prices",
            "movie reviews",
            "travel destinations",
            "programming languages"
        };

        // Act & Assert
        foreach (var query in nonRecipeQueries)
        {
            var request = new NLWebRequest { Query = query };
            var canHandle = _recipeToolHandler.CanHandle(request);
            Assert.IsFalse(canHandle, $"Should not handle non-recipe query: {query}");
        }
    }

    [TestMethod]
    public void CanHandle_WithEmptyQuery_ReturnsFalse()
    {
        // Arrange
        var requests = new[]
        {
            new NLWebRequest { Query = "" },
            new NLWebRequest { Query = null! },
            new NLWebRequest { Query = "   " }
        };

        // Act & Assert
        foreach (var request in requests)
        {
            var canHandle = _recipeToolHandler.CanHandle(request);
            Assert.IsFalse(canHandle, "Should not handle empty or null queries");
        }
    }

    [TestMethod]
    public void GetPriority_WithSubstitutionQuery_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "substitute for butter in cookies" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(95, priority);
    }

    [TestMethod]
    public void GetPriority_WithRecipeForQuery_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "recipe for chocolate chip cookies" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithHowToCookQuery_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "how to cook perfect rice" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithServeWithQuery_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "what to serve with roast beef" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithGoesWithQuery_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "wine that goes with seafood" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithGeneralCookingQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "cooking techniques for beginners" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(70, priority);
    }

    [TestMethod]
    public void GetPriority_WithBakingQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "baking tips for professionals" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(70, priority);
    }

    [TestMethod]
    public void GetPriority_WithIngredientQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest { Query = "fresh ingredient storage tips" };

        // Act
        var priority = _recipeToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(70, priority);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFaultyResultGenerator_ReturnsErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recipe for disaster",
            Mode = QueryMode.List,
            QueryId = "test-recipe-error-001"
        };

        var faultyResultGenerator = new FaultyTestResultGenerator();
        var faultyHandler = new RecipeToolHandler(
            _logger,
            _options,
            _queryProcessor,
            faultyResultGenerator);

        // Act
        var response = await faultyHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.Contains("Recipe tool execution failed", response.Error);
        Assert.AreEqual(1, response.Results.Count); // Error result
        Assert.AreEqual("Tool Error", response.Results[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_ValidatesResponseStructure()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recipe for validation test",
            Mode = QueryMode.List,
            QueryId = "test-recipe-validation-001"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Test Recipe", Description = "Validation test recipe", Score = 0.8 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.AreEqual(request.Mode, response.Mode);
        Assert.IsNotNull(response.Results);
        Assert.IsNotNull(response.ProcessingTimeMs);
        Assert.IsGreaterThanOrEqualTo(response.ProcessingTimeMs.Value, 0);
        Assert.IsLessThanOrEqualTo(response.Timestamp, DateTimeOffset.UtcNow);
        Assert.IsGreaterThan(response.Timestamp, DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [TestMethod]
    public async Task ExecuteAsync_EnsuresRecipeGuideHeader()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "special cooking technique",
            Mode = QueryMode.List,
            QueryId = "test-recipe-header-001"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Cooking Technique", Description = "Special technique guide", Score = 0.9 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsGreaterThanOrEqualTo(response.Results.Count, 1);

        var headerResult = response.Results.FirstOrDefault(r => r.Name.Contains("Recipe Guide"));
        Assert.IsNotNull(headerResult, "Should include Recipe Guide header");
        Assert.Contains("special cooking technique", headerResult.Description);
        Assert.AreEqual("Recipe", headerResult.Site);
        Assert.AreEqual(1.0, headerResult.Score, 0.001);
    }

    [TestMethod]
    public async Task ExecuteAsync_FiltersRelevantResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "ingredient storage tips",
            Mode = QueryMode.List,
            QueryId = "test-recipe-filter-001"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult { Name = "Storage Guide", Description = "Ingredient storage methods", Score = 0.9 },
            new NLWebResult { Name = "Unrelated Result", Description = "Random unrelated content", Score = 0.5 },
            new NLWebResult { Name = "Food Safety", Description = "Safe food storage practices", Score = 0.8 }
        });

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsGreaterThan(response.Results.Count, 1); // Header + filtered results

        // Should include relevant results but filter out unrelated ones
        var relevantResults = response.Results.Where(r => !r.Name.Contains("Recipe Guide")).ToList();
        var hasMatchingItem = relevantResults.Any(r => r.Name.Contains("Storage") || r.Name.Contains("Food"));
        Assert.IsTrue(hasMatchingItem);
    }

    [TestMethod]
    public async Task ExecuteAsync_LimitsResultsToEight()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "comprehensive cooking guide",
            Mode = QueryMode.List,
            QueryId = "test-recipe-limit-001"
        };

        // Generate 15 results to test the limit of 8
        var manyResults = Enumerable.Range(1, 15)
            .Select(i => new NLWebResult
            {
                Name = $"Recipe Result {i}",
                Description = $"Cooking guide part {i}",
                Score = 0.9 - (i * 0.01)
            })
            .ToArray();

        _resultGenerator.SetResults(manyResults);

        // Act
        var response = await _recipeToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);

        // Should have header + max 8 recipe results = 9 total
        Assert.IsLessThanOrEqualTo(response.Results.Count, 9);

        var recipeResults = response.Results.Where(r => !r.Name.Contains("Recipe Guide")).ToList();
        Assert.IsLessThanOrEqualTo(recipeResults.Count, 8, "Should limit recipe results to 8");
    }
}
