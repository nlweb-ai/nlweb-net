using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Services;

[TestClass]
public class CompareToolHandlerTests
{
    private CompareToolHandler _compareToolHandler = null!;
    private TestLogger<CompareToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<CompareToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _compareToolHandler = new CompareToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void ToolType_ReturnsCorrectType()
    {
        // Act
        var toolType = _compareToolHandler.ToolType;

        // Assert
        Assert.AreEqual("compare", toolType);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBasicCompareQuery_ReturnsComparisonResults()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare React vs Angular",
            Mode = QueryMode.List,
            QueryId = "test-compare-001"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "React vs Angular Comparison",
                Description = "Detailed comparison between React and Angular frameworks",
                Url = "https://example.com/react-vs-angular",
                Score = 95.0
            },
            new NLWebResult
            {
                Name = "Frontend Framework Guide",
                Description = "Comprehensive guide to React and Angular differences",
                Url = "https://example.com/frontend-frameworks",
                Score = 88.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsGreaterThanOrEqualTo(1, response.Results.Count);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.Contains("react vs angular comparison differences", response.ProcessedQuery!);
        Assert.IsNotNull(response.Summary);
        Assert.Contains("Comparison completed between 'react' and 'angular'", response.Summary!);
        Assert.IsNotNull(response.ProcessingTimeMs);
        Assert.IsGreaterThan(0, response.ProcessingTimeMs.Value);

        // Verify comparison structure - should have summary comparison result
        var resultsList = response.Results.ToList();
        var hasMatchingItem = resultsList.Any(r => r.Name?.StartsWith("Comparison:") == true);
        Assert.IsTrue(hasMatchingItem);
        var hasComparisonSite = resultsList.Any(r => r.Site == "Compare");
        Assert.IsTrue(hasComparisonSite);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithVersusQuery_ExtractsItemsCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "Python versus Java",
            Mode = QueryMode.List,
            QueryId = "test-compare-002"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Python vs Java",
                Description = "Programming language comparison between Python and Java",
                Url = "https://example.com/python-vs-java",
                Score = 92.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.Contains("python vs java", response.ProcessedQuery!);
        Assert.IsNotNull(response.Summary);
        Assert.Contains("'python' and 'java'", response.Summary!);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDifferenceBetweenQuery_ProcessesCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "difference between SQL and NoSQL",
            Mode = QueryMode.List,
            QueryId = "test-compare-003"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "SQL vs NoSQL Databases",
                Description = "Key differences between SQL and NoSQL database systems",
                Url = "https://example.com/sql-vs-nosql",
                Score = 89.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.ProcessedQuery);
        Assert.Contains("sql vs nosql", response.ProcessedQuery!);
        Assert.IsNotNull(response.Summary);
        Assert.Contains("'sql' and 'nosql'", response.Summary!);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyResults_ReturnsComparisonSummaryOnly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare item1 vs item2",
            Mode = QueryMode.List,
            QueryId = "test-compare-004"
        };

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.HasCount(1, response.Results); // Only the comparison summary

        var resultsList = response.Results.ToList();
        Assert.AreEqual("Comparison: item1 vs item2", resultsList[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithUnidentifiableItems_ReturnsError()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare something",
            Mode = QueryMode.List,
            QueryId = "test-compare-005"
        };

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.Contains("Could not identify two items to compare", response.Error!);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare React vs Vue",
            Mode = QueryMode.List,
            QueryId = "test-compare-006"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _resultGenerator.SetResults(Array.Empty<NLWebResult>());

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request, cts.Token);

        // Assert - The handler should complete successfully even with cancellation
        // since our test doubles don't actually respect cancellation tokens
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
    }

    [TestMethod]
    public void CanHandle_WithCompareKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare React and Angular",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithVsKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "Python vs Java performance",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithVersusKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "iOS versus Android",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithDifferenceKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "difference between REST and GraphQL",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithDifferencesKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "key differences in approaches",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithContrastKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "contrast these two solutions",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithBetterKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "which is better for web development",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithWorseKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is worse about this approach",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithProsAndConsKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "pros and cons of microservices",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithWhichIsBetterKeyword_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "which is better for machine learning",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithNonCompareQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about machine learning",
            Mode = QueryMode.List
        };

        // Act
        var canHandle = _compareToolHandler.CanHandle(request);

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
        var canHandle = _compareToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void GetPriority_WithVsQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "React vs Angular",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(95, priority);
    }

    [TestMethod]
    public void GetPriority_WithVersusQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "Python versus Java",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(95, priority);
    }

    [TestMethod]
    public void GetPriority_WithCompareStartQuery_ReturnsHighestPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare these two frameworks",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(95, priority);
    }

    [TestMethod]
    public void GetPriority_WithDifferenceQuery_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "difference between REST and SOAP",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithContrastQuery_ReturnsHighPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "contrast these approaches",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(85, priority);
    }

    [TestMethod]
    public void GetPriority_WithGeneralCompareQuery_ReturnsMediumPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "which is better for development",
            Mode = QueryMode.List
        };

        // Act
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(70, priority);
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
        var priority = _compareToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(70, priority);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSiteParameter_PassesSiteToResultGenerator()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare Azure vs AWS",
            Site = "microsoft.com",
            Mode = QueryMode.List,
            QueryId = "test-compare-007"
        };

        _resultGenerator.SetResults(new[]
        {
            new NLWebResult
            {
                Name = "Azure vs AWS Comparison",
                Description = "Microsoft Azure compared to Amazon Web Services",
                Url = "https://azure.microsoft.com/comparison",
                Score = 95.0
            }
        });

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsGreaterThanOrEqualTo(2, response.Results.Count); // Summary + at least one result
    }

    [TestMethod]
    public async Task ExecuteAsync_WithManyResults_LimitsResultsCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare frameworks vs libraries",
            Mode = QueryMode.List,
            QueryId = "test-compare-008"
        };

        // Create 15 test results
        var testResults = Enumerable.Range(1, 15)
            .Select(i => new NLWebResult
            {
                Name = $"Framework {i}",
                Description = $"Comparison details for framework {i} versus libraries",
                Url = $"https://example.com/framework{i}",
                Score = 100.0 - i
            })
            .ToArray();

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);
        Assert.IsLessThanOrEqualTo(9, response.Results.Count, "Should limit to 8 comparison results + 1 summary = 9 maximum");

        // Should always have the comparison summary as first result
        var resultsList = response.Results.ToList();
        Assert.AreEqual("Comparison: frameworks vs libraries", resultsList[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProcessorException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare something vs something",
            Mode = QueryMode.List,
            QueryId = "test-compare-009"
        };

        // Configure faulty result generator to throw exception
        var faultyGenerator = new FaultyTestResultGenerator();
        var faultyHandler = new CompareToolHandler(
            _logger,
            _options,
            _queryProcessor,
            faultyGenerator);

        // Act
        var response = await faultyHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.Contains("Compare tool execution failed", response.Error!);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithRelevantResults_FiltersCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare React vs Angular",
            Mode = QueryMode.List,
            QueryId = "test-compare-010"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "React Framework Guide",
                Description = "Complete guide to React development",
                Url = "https://example.com/react",
                Score = 90.0
            },
            new NLWebResult
            {
                Name = "Angular Tutorial",
                Description = "Learn Angular framework from scratch",
                Url = "https://example.com/angular",
                Score = 85.0
            },
            new NLWebResult
            {
                Name = "Database Design",
                Description = "How to design databases effectively",
                Url = "https://example.com/database",
                Score = 95.0  // Higher score but not relevant
            },
            new NLWebResult
            {
                Name = "React vs Angular Comparison",
                Description = "Direct comparison between React and Angular",
                Url = "https://example.com/react-vs-angular",
                Score = 88.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Results);

        var resultsList = response.Results.ToList();

        // Should have comparison summary + relevant results (not the database one)
        Assert.IsGreaterThanOrEqualTo(2, resultsList.Count);
        Assert.IsLessThanOrEqualTo(5, resultsList.Count); // Summary + up to 4 relevant results

        // Should filter out irrelevant results (database)
        var containsDatabase = resultsList.Any(r => r.Name?.Contains("Database") == true);
        Assert.IsFalse(containsDatabase);

        var hasReactItem = resultsList.Any(r => r.Name?.ToLowerInvariant().Contains("react") == true);
        var hasAngularItem = resultsList.Any(r => r.Name?.ToLowerInvariant().Contains("angular") == true);
        Assert.IsTrue(hasReactItem);
        Assert.IsTrue(hasAngularItem);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithComplexComparisonQuery_ExtractsCorrectly()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare Node.js backend development versus PHP server-side programming",
            Mode = QueryMode.List,
            QueryId = "test-compare-011"
        };

        var testResults = new[]
        {
            new NLWebResult
            {
                Name = "Node.js vs PHP Performance",
                Description = "Performance comparison between Node.js and PHP",
                Url = "https://example.com/nodejs-vs-php",
                Score = 92.0
            }
        };

        _resultGenerator.SetResults(testResults);

        // Act
        var response = await _compareToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.ProcessedQuery);

        // Should extract the main technologies being compared
        var processedQueryContainsNodejs = response.ProcessedQuery.Contains("node.js") || response.ProcessedQuery.Contains("nodejs");
        Assert.IsTrue(processedQueryContainsNodejs);
        var processedQueryContainsPhp = response.ProcessedQuery.Contains("php");
        Assert.IsTrue(processedQueryContainsPhp);
        var summaryContainsNodejs = response.Summary?.Contains("node.js") == true || response.Summary?.Contains("nodejs") == true;
        Assert.IsTrue(summaryContainsNodejs);
        var summaryContainsPhp = response.Summary?.Contains("php") == true;
        Assert.IsTrue(summaryContainsPhp);
    }
}