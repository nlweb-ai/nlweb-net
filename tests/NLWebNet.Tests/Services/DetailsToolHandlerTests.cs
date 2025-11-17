using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Services;

[TestClass]
public class DetailsToolHandlerTests
{
    private DetailsToolHandler _detailsToolHandler = null!;
    private TestLogger<DetailsToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<DetailsToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _detailsToolHandler = new DetailsToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void ToolType_ReturnsCorrectType()
    {
        // Act
        var toolType = _detailsToolHandler.ToolType;

        // Assert
        Assert.AreEqual("details", toolType);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBasicDetailsQuery_ReturnsDetailedResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about machine learning",
            Mode = QueryMode.List,
            QueryId = "test-details-001"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Machine Learning Overview",
                Description = "Comprehensive introduction to machine learning concepts and algorithms",
                Url = "https://example.com/ml-overview",
                Score = 95.0
            },
            new NLWebResult
            {
                Name = "ML Fundamentals",
                Description = "Basic principles and foundations of machine learning",
                Url = "https://example.com/ml-fundamentals",
                Score = 88.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsGreaterThan(response.Results.Count , = 1);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.IsTrue(response.ProcessedQuery.Contains("machine learning overview definition explanation details"));
        Assert.Contains("Details retrieved for 'machine learning'", response.Summary);
        Assert.IsGreaterThan(response.ProcessingTimeMs , 0);

        // Verify details enhancement
        var resultsList = response.Results.ToList();
        Assert.IsTrue(resultsList.Any(r => r.Name?.StartsWith("Details:") == true));
    }

    [TestMethod]
    public async Task ExecuteAsync_WithWhatIsQuery_ExtractsSubjectCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is artificial intelligence",
            Mode = QueryMode.List,
            QueryId = "test-details-002"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "AI Definition",
                Description = "Artificial intelligence overview and explanation",
                Url = "https://example.com/ai-definition",
                Score = 92.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.IsTrue(response.ProcessedQuery.Contains("artificial intelligence"));
        Assert.Contains("artificial intelligence", response.Summary);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInformationAboutQuery_ProcessesCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "information about cloud computing",
            Mode = QueryMode.List,
            QueryId = "test-details-003"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Cloud Computing Guide",
                Description = "Complete guide to cloud computing technologies and services",
                Url = "https://example.com/cloud-guide",
                Score = 89.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.Contains("cloud computing", response.ProcessedQuery);
        Assert.Contains("cloud computing", response.Summary);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyResults_ReturnsEmptyDetailsList()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about non-existent technology",
            Mode = QueryMode.List,
            QueryId = "test-details-004"
        };

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.HasCount(response.Results, 0);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithUnidentifiableSubject_ReturnsError()
    {
        // Arrange - Use empty string which will fail the subject extraction check
        var request = new NLWebRequest
        {
            Query = "",
            Mode = QueryMode.List,
            QueryId = "test-details-005"
        };

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.Contains("Could not identify the subject", response.Error);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about databases",
            Mode = QueryMode.List,
            QueryId = "test-details-006"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request, cts.Token);

        // Assert - The handler should complete successfully even with cancellation
        // since our test doubles don't actually respect cancellation tokens
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
    }

    [TestMethod]
    public void CanHandle_WithTellMeAboutKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about machine learning",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithWhatIsKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is artificial intelligence",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithInformationAboutKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "information about cloud computing",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithDescribeKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "describe how APIs work",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithDetailsKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "details about web development",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithExplainKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "explain quantum computing",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithDefinitionKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "definition of blockchain",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithOverviewKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "overview of microservices",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithNonDetailsQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for programming tutorials",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _detailsToolHandler.CanHandle(request);

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
        var canHandle = _detailsToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void GetPriority_WithTellMeAboutQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about machine learning",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithWhatIsQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is artificial intelligence",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithDetailsAboutQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "details about cloud computing",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(90, priority);
    }

    [TestMethod]
    public void GetPriority_WithInformationAboutQuery_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "information about databases",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(75, priority);
    }

    [TestMethod]
    public void GetPriority_WithDescribeQuery_ReturnsMediumHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "describe how REST APIs work",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(75, priority);
    }

    [TestMethod]
    public void GetPriority_WithGeneralDetailsQuery_ReturnsDefaultPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "details on programming",
            Mode = QueryMode.List
        };

        // Act
        var priority = _detailsToolHandler.GetPriority(request);

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
        var priority = _detailsToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(65, priority);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSiteParameter_PassesSiteToResultGenerator()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about Microsoft Azure",
            Site = "microsoft.com",
            Mode = QueryMode.List,
            QueryId = "test-details-007"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "Azure Overview",
                Description = "Microsoft Azure cloud platform overview",
                Url = "https://azure.microsoft.com/overview",
                Score = 95.0
            }
        });

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsGreaterThan(response.Results.Count , = 1);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithManyResults_LimitsToTenResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about programming languages",
            Mode = QueryMode.List,
            QueryId = "test-details-008"
        };

        // Create 15 test results
        var testResults = Enumerable.Range(1, 15)
            .Select(i => new NLWebResult
            {
                Name = $"Programming Language {i}",
                Description = $"Overview and details about programming language {i}",
                Url = $"https://example.com/lang{i}",
                Score = 100.0 - i
            })
            .ToArray();

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsLessThan(response.Results.Count , = 10, "Should limit results to 10 maximum");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProcessorException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about something",
            Mode = QueryMode.List,
            QueryId = "test-details-009"
        };

        // Configure faulty result generator to throw exception
        var faultyGenerator = new FaultyTestResultGenerator();
        var faultyHandler = new DetailsToolHandler(
            _logger,
            _options,
            _queryProcessor,
            faultyGenerator);

        // Act
        var response = await faultyHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.Contains("Details tool execution failed", response.Error);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithRelevanceCalculation_RanksResultsCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about machine learning",
            Mode = QueryMode.List,
            QueryId = "test-details-010"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Machine Learning Guide",
                Description = "Complete machine learning overview and introduction",
                Url = "https://example.com/ml-guide",
                Score = 80.0
            },
            new NLWebResult
            {
                Name = "Basic Programming",
                Description = "Programming fundamentals",
                Url = "https://example.com/programming",
                Score = 85.0  // Higher base score but less relevant
            },
            new NLWebResult
            {
                Name = "ML Overview",
                Description = "Machine learning definition and explanation",
                Url = "https://example.com/ml-overview",
                Score = 75.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _detailsToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);

        // Results should be reordered by relevance, not just base score
        var resultsList = response.Results.ToList();
        Assert.IsGreaterThan(resultsList.Count , = 2);

        // ML-related results should be ranked higher due to relevance
        var topResult = resultsList.FirstOrDefault();
        Assert.IsNotNull(topResult);
        Assert.IsTrue(topResult.Name?.ToLowerInvariant().Contains("machine learning") == true ||
                     topResult.Name?.ToLowerInvariant().Contains("ml") == true);
    }
}