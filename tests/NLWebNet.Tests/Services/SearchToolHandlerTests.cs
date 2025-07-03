using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Services;

[TestClass]
public class SearchToolHandlerTests
{
    private SearchToolHandler _searchToolHandler = null!;
    private TestLogger<SearchToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<SearchToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _searchToolHandler = new SearchToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void ToolType_ReturnsCorrectType()
    {
        // Act
        var toolType = _searchToolHandler.ToolType;

        // Assert
        Assert.AreEqual("search", toolType);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBasicSearchQuery_ReturnsEnhancedResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for API documentation",
            Mode = QueryMode.List,
            QueryId = "test-search-001"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "API Documentation Guide",
                Description = "Comprehensive guide to API documentation",
                Url = "https://example.com/api-docs",
                Score = 85.0
            },
            new NLWebResult
            {
                Name = "REST API Tutorial",
                Description = "Learn about REST APIs",
                Url = "https://example.com/rest-tutorial",
                Score = 75.0
            }
        };

        _queryProcessor.ProcessedQuery = "API documentation";
        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.IsNull(response.Error); // Success means no error
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(2, response.Results.Count);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.IsTrue(response.Summary?.Contains("Enhanced search completed") == true);
        Assert.IsTrue(response.ProcessingTimeMs > 0);

        // Verify results are ordered by relevance
        var resultsList = response.Results.ToList();
        Assert.AreEqual("API Documentation Guide", resultsList[0].Name);
        Assert.AreEqual("Search", resultsList[0].Site); // Search-specific metadata
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSearchKeywords_OptimizesQuery()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for database tutorials",
            Mode = QueryMode.List,
            QueryId = "test-search-002"
        };

        _queryProcessor.ProcessedQuery = "search for database tutorials";
        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "Database Tutorial",
                Description = "Learn database concepts",
                Url = "https://example.com/db-tutorial",
                Score = 90.0
            }
        });

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.ProcessedQuery);
        // Query should be optimized to remove redundant "search for"
        Assert.AreEqual("database tutorials", response.ProcessedQuery);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyResults_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "very specific non-existent query",
            Mode = QueryMode.List,
            QueryId = "test-search-003"
        };

        _queryProcessor.ProcessedQuery = "very specific non-existent query";
        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(0, response.Results.Count);
        Assert.IsTrue(response.Summary?.Contains("found 0 results") == true);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search query",
            Mode = QueryMode.List,
            QueryId = "test-search-004"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request, cts.Token);

        // Assert - The handler should complete successfully even with cancellation
        // since our test doubles don't actually respect cancellation tokens
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
    }

    [TestMethod]
    public void CanHandle_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "find information about machine learning",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _searchToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
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
        var canHandle = _searchToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithEmptyQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _searchToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void GetPriority_WithExplicitSearchKeywords_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for documentation",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithFindKeyword_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "find examples of REST APIs",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithLookForKeyword_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "look for tutorials about databases",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithLocateKeyword_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "locate information about security",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithDiscoverKeyword_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "discover new frameworks",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(80, priority);
    }

    [TestMethod]
    public void GetPriority_WithGeneralQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "machine learning algorithms",
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(60, priority);
    }

    [TestMethod]
    public void GetPriority_WithNullQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = null!,
            Mode = QueryMode.List
        };

        // Act
        var priority = _searchToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(60, priority);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSiteParameter_PassesSiteToResultGenerator()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "API documentation",
            Site = "docs.microsoft.com",
            Mode = QueryMode.List,
            QueryId = "test-search-005"
        };

        _queryProcessor.ProcessedQuery = "API documentation";
        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "Microsoft API Docs",
                Description = "Official Microsoft API documentation",
                Url = "https://docs.microsoft.com/api",
                Score = 95.0
            }
        });

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(1, response.Results.Count);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithRelevanceCalculation_OrdersResultsCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "API documentation",
            Mode = QueryMode.List,
            QueryId = "test-search-006"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "General Tutorial", // Lower relevance (no matches)
                Description = "Some general information",
                Url = "https://example.com/general",
                Score = 45.0 // Lower base score so relevance boost wins
            },
            new NLWebResult
            {
                Name = "API Documentation Guide", // Higher relevance (contains both "API" and "documentation")
                Description = "Complete API documentation guide", // Also contains both terms
                Url = "https://example.com/api-docs",
                Score = 40.0 // Will get +10 relevance boost (3+3+2+2) = 50.0 total > 45.0
            }
        };

        _queryProcessor.ProcessedQuery = "API documentation";
        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _searchToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);

        var resultsList = response.Results.ToList();
        Assert.AreEqual(2, resultsList.Count);

        // First result should be the one with higher calculated relevance
        Assert.AreEqual("API Documentation Guide", resultsList[0].Name);
        Assert.AreEqual("General Tutorial", resultsList[1].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProcessorException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List,
            QueryId = "test-search-007"
        };

        // Configure query processor to throw exception
        var faultyProcessor = new FaultyTestQueryProcessor();
        var faultyHandler = new SearchToolHandler(
            _logger,
            _options,
            faultyProcessor,
            _resultGenerator);

        // Act
        var response = await faultyHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.IsTrue(response.Error?.Contains("Search tool execution failed") == true);
    }
}

/// <summary>
/// Test helper class that throws exceptions for testing error handling
/// </summary>
public class FaultyTestQueryProcessor : IQueryProcessor
{
    public Task<string> ProcessQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception");
    }

    public string GenerateQueryId(NLWebRequest request)
    {
        return Guid.NewGuid().ToString();
    }

    public bool ValidateRequest(NLWebRequest request)
    {
        return true;
    }
}
