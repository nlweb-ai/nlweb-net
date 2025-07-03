using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;
using System.Runtime.CompilerServices;

namespace NLWebNet.Tests.Services;

[TestClass]
public class EnsembleToolHandlerTests
{
    private EnsembleToolHandler _ensembleToolHandler = null!;
    private TestLogger<EnsembleToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<EnsembleToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _ensembleToolHandler = new EnsembleToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void ToolType_ReturnsCorrectType()
    {
        // Act
        var toolType = _ensembleToolHandler.ToolType;

        // Assert
        Assert.AreEqual("ensemble", toolType);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBasicEnsembleQuery_ReturnsEnsembleResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend a set of tools for development",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-001"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Visual Studio Code",
                Description = "Powerful code editor",
                Url = "https://code.visualstudio.com",
                Score = 95.0
            },
            new NLWebResult
            {
                Name = "Git",
                Description = "Version control system",
                Url = "https://git-scm.com",
                Score = 90.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(3, response.Results.Count); // 1 overview + 2 options
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.IsTrue(response.ProcessedQuery?.Contains("recommendations suggestions set") == true);
        Assert.IsTrue(response.Summary?.Contains("Ensemble recommendations created") == true);
        Assert.IsTrue(response.ProcessingTimeMs > 0);

        // Verify ensemble structure
        var resultsList = response.Results.ToList();
        Assert.AreEqual("Curated Ensemble Recommendations", resultsList[0].Name);
        Assert.AreEqual("Ensemble", resultsList[0].Site);
        Assert.AreEqual("[Option 1] Visual Studio Code", resultsList[1].Name);
        Assert.AreEqual("[Option 2] Git", resultsList[2].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithPlanningQuery_ProcessesCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "plan a day in Seattle with museums and restaurants",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-002"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Seattle Art Museum",
                Description = "Contemporary and modern art",
                Url = "https://seattleartmuseum.org",
                Score = 88.0
            },
            new NLWebResult
            {
                Name = "Pike Place Market",
                Description = "Historic public market",
                Url = "https://pikeplacemarket.org",
                Score = 92.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(3, response.Results.Count);
        
        var resultsList = response.Results.ToList();
        Assert.IsTrue(resultsList[0].Description?.Contains("plan a day in Seattle") == true);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyResults_ReturnsEnsembleOverviewOnly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "give me non-existent items",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-003"
        };

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(1, response.Results.Count); // Only the overview
        
        var resultsList = response.Results.ToList();
        Assert.AreEqual("Curated Ensemble Recommendations", resultsList[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend something",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-004"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request, cts.Token);

        // Assert - The handler should complete successfully even with cancellation
        // since our test doubles don't actually respect cancellation tokens
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
    }

    [TestMethod]
    public void CanHandle_WithRecommendKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend some tools for development",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithSuggestKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "suggest some places to visit",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithGiveMeKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "give me a list of frameworks",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithPlanKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "plan a vacation itinerary",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithSetOfKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "I need a set of tools",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithEnsembleKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "create an ensemble of related items",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithWhatShouldIKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what should i use for web development",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithHelpMeChooseKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "help me choose the best options",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithINeedKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "i need some recommendations",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithLookingForKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "looking for a complete solution",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithNonEnsembleQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is machine learning",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithNullQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = null!,
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _ensembleToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void GetPriority_WithGiveMeAndMultipleItems_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "give me an appetizer, main course, and dessert",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithGiveMeAndCommaItems_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "give me tools, frameworks, libraries",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithPlanKeyword_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "plan my vacation itinerary",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithHelpMePlan_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "help me plan a dinner party",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithRecommendAndMultipleItems_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend databases and web frameworks",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithSuggestAndMultipleItems_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "suggest tools, libraries, and frameworks",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithGeneralEnsembleQuery_ReturnsDefaultPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend a framework",
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(65, priority);
    }

    [TestMethod]
    public void GetPriority_WithNullQuery_ReturnsDefaultPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = null!,
            Mode = QueryMode.List
        };

        // Act
        var priority = _ensembleToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(65, priority);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSiteParameter_PassesSiteToResultGenerator()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend development tools",
            Site = "github.com",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-005"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "GitHub Actions",
                Description = "CI/CD platform",
                Url = "https://github.com/features/actions",
                Score = 95.0
            }
        });

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(2, response.Results.Count);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithManyResults_LimitsToTenOptions()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend many tools",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-006"
        };

        // Create 15 test results
        var testResults = Enumerable.Range(1, 15)
            .Select(i => new NLWebResult
            {
                Name = $"Tool {i}",
                Description = $"Description for tool {i}",
                Url = $"https://example.com/tool{i}",
                Score = 100.0 - i
            })
            .ToArray();

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(11, response.Results.Count); // 1 overview + 10 options (limited)
        
        var resultsList = response.Results.ToList();
        Assert.AreEqual("Curated Ensemble Recommendations", resultsList[0].Name);
        Assert.AreEqual("[Option 1] Tool 1", resultsList[1].Name);
        Assert.AreEqual("[Option 10] Tool 10", resultsList[10].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProcessorException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend something",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-007"
        };

        // Configure faulty result generator to throw exception
        var faultyGenerator = new FaultyTestResultGenerator();
        var faultyHandler = new EnsembleToolHandler(
            _logger,
            _options,
            _queryProcessor,
            faultyGenerator);

        // Act
        var response = await faultyHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.IsTrue(response.Error?.Contains("Ensemble tool execution failed") == true);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithQueryEnhancement_AddsEnsembleTerms()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "give me development tools",
            Mode = QueryMode.List,
            QueryId = "test-ensemble-008"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "Visual Studio",
                Description = "IDE for development",
                Url = "https://visualstudio.microsoft.com",
                Score = 90.0
            }
        });

        // Act
        var response = await _ensembleToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.AreEqual("give me development tools recommendations suggestions set", response.ProcessedQuery);
    }
}

/// <summary>
/// Test helper class that throws exceptions for testing error handling
/// </summary>
public class FaultyTestResultGenerator : IResultGenerator
{
    public Task<IEnumerable<NLWebResult>> GenerateListAsync(string query, string? site = null, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception");
    }

    public Task<(string Summary, IEnumerable<NLWebResult> Results)> GenerateSummaryAsync(string query, IEnumerable<NLWebResult> results, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception");
    }

    public Task<(string GeneratedResponse, IEnumerable<NLWebResult> Results)> GenerateResponseAsync(string query, IEnumerable<NLWebResult> results, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception");
    }

    public IAsyncEnumerable<string> GenerateStreamingResponseAsync(string query, IEnumerable<NLWebResult> results, QueryMode mode, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception");
    }
}
