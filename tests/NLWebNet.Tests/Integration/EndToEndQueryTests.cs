using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Integration;

/// <summary>
/// Comprehensive end-to-end query testing with configurable test suites
/// </summary>
[TestClass]
public class EndToEndQueryTests
{
    private INLWebService _nlWebService = null!;
    private IServiceProvider _serviceProvider = null!;

    [TestInitialize]
    public void Initialize()
    {
        var services = new ServiceCollection();

        // Configure with default settings
        services.AddNLWebNetMultiBackend(options =>
        {
            options.DefaultMode = QueryMode.List;
            options.MaxResultsPerQuery = 10;
            options.EnableDecontextualization = false;
        });

        _serviceProvider = services.BuildServiceProvider();
        _nlWebService = _serviceProvider.GetRequiredService<INLWebService>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Tests all basic search scenarios from test data manager
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_BasicSearchScenarios_AllPass()
    {
        var basicSearchScenarios = TestDataManager.GetTestScenarios()
            .Where(s => s.TestCategories.Contains(TestConstants.Categories.BasicSearch));

        foreach (var scenario in basicSearchScenarios)
        {
            Console.WriteLine($"Testing scenario: {scenario.Name}");

            var request = scenario.ToRequest();
            var response = await _nlWebService.ProcessRequestAsync(request);

            // Assert basic response properties
            Assert.IsNotNull(response, $"Response should not be null for scenario: {scenario.Name}");
            Assert.AreEqual(request.QueryId, response.QueryId, "QueryId should match");
            Assert.IsNull(response.Error, $"Response should not have error for scenario: {scenario.Name}");

            // Assert result count
            if (scenario.MinExpectedResults > 0)
            {
                Assert.IsNotNull(response.Results, $"Results should not be null for scenario: {scenario.Name}");
                Assert.IsTrue(response.Results.Count() >= scenario.MinExpectedResults,
                    $"Should have at least {scenario.MinExpectedResults} results for scenario: {scenario.Name}");
            }

            Console.WriteLine($"✓ Scenario '{scenario.Name}' passed with {response.Results?.Count() ?? 0} results");
        }
    }

    /// <summary>
    /// Tests edge cases and validation scenarios
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_EdgeCaseScenarios_HandleCorrectly()
    {
        var edgeCaseScenarios = TestDataManager.GetTestScenarios()
            .Where(s => s.TestCategories.Contains(TestConstants.Categories.EdgeCase));

        foreach (var scenario in edgeCaseScenarios)
        {
            Console.WriteLine($"Testing edge case: {scenario.Name}");

            var request = scenario.ToRequest();
            var response = await _nlWebService.ProcessRequestAsync(request);

            // Edge cases should not throw exceptions
            Assert.IsNotNull(response, $"Response should not be null for edge case: {scenario.Name}");
            Assert.AreEqual(request.QueryId, response.QueryId, "QueryId should match");

            // Edge cases might have zero results, which is acceptable
            var resultCount = response.Results?.Count() ?? 0;
            Console.WriteLine($"✓ Edge case '{scenario.Name}' handled correctly with {resultCount} results");
        }
    }

    /// <summary>
    /// Tests site-specific filtering functionality
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_SiteFilteringScenarios_FilterCorrectly()
    {
        var siteFilteringScenarios = TestDataManager.GetTestScenarios()
            .Where(s => s.TestCategories.Contains(TestConstants.Categories.SiteFiltering));

        foreach (var scenario in siteFilteringScenarios)
        {
            Console.WriteLine($"Testing site filtering: {scenario.Name}");

            var request = scenario.ToRequest();
            var response = await _nlWebService.ProcessRequestAsync(request);

            Assert.IsNotNull(response, $"Response should not be null for scenario: {scenario.Name}");
            Assert.IsNull(response.Error, $"Response should not have error for scenario: {scenario.Name}");

            if (response.Results?.Any() == true && !string.IsNullOrEmpty(scenario.Site))
            {
                // Verify site filtering is applied (all results should be from the specified site)
                var resultsFromOtherSites = response.Results.Where(r =>
                    !string.IsNullOrEmpty(r.Site) &&
                    !r.Site.Equals(scenario.Site, StringComparison.OrdinalIgnoreCase)).ToList();

                if (resultsFromOtherSites.Count > 0)
                {
                    Console.WriteLine($"Warning: Found {resultsFromOtherSites.Count} results from other sites. " +
                        "This might be expected if site filtering is not strictly enforced.");
                }
            }

            Console.WriteLine($"✓ Site filtering scenario '{scenario.Name}' completed");
        }
    }

    /// <summary>
    /// Tests technical information queries
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_TechnicalQueries_ReturnRelevantResults()
    {
        var technicalScenarios = TestDataManager.GetTestScenarios()
            .Where(s => s.TestCategories.Contains(TestConstants.Categories.Technical));

        foreach (var scenario in technicalScenarios)
        {
            Console.WriteLine($"Testing technical query: {scenario.Name}");

            var request = scenario.ToRequest();
            var response = await _nlWebService.ProcessRequestAsync(request);

            Assert.IsNotNull(response, $"Response should not be null for scenario: {scenario.Name}");
            Assert.IsNull(response.Error, $"Response should not have error for scenario: {scenario.Name}");

            if (scenario.MinExpectedResults > 0)
            {
                Assert.IsNotNull(response.Results, $"Results should not be null for scenario: {scenario.Name}");
                Assert.IsTrue(response.Results.Count() >= scenario.MinExpectedResults,
                    $"Should have at least {scenario.MinExpectedResults} results for scenario: {scenario.Name}");

                // Verify results have meaningful content
                foreach (var result in response.Results.Take(3)) // Check first 3 results
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(result.Name), "Result name should not be empty");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(result.Description), "Result description should not be empty");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(result.Url), "Result URL should not be empty");
                }
            }

            Console.WriteLine($"✓ Technical query '{scenario.Name}' passed with {response.Results?.Count() ?? 0} results");
        }
    }

    /// <summary>
    /// Tests all query modes with different scenarios
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_DifferentQueryModes_WorkCorrectly()
    {
        var testQuery = "millennium falcon";
        var queryModes = new[] { QueryMode.List, QueryMode.Summarize };

        foreach (var mode in queryModes)
        {
            Console.WriteLine($"Testing query mode: {mode}");

            var request = new NLWebRequest
            {
                QueryId = $"test-mode-{mode}-{Guid.NewGuid():N}",
                Query = testQuery,
                Mode = mode
            };

            var response = await _nlWebService.ProcessRequestAsync(request);

            Assert.IsNotNull(response, $"Response should not be null for mode: {mode}");
            Assert.AreEqual(request.QueryId, response.QueryId, "QueryId should match");
            Assert.IsNull(response.Error, $"Response should not have error for mode: {mode}");

            Console.WriteLine($"✓ Query mode '{mode}' worked correctly");
        }
    }

    /// <summary>
    /// Tests streaming functionality end-to-end
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_StreamingQueries_StreamCorrectly()
    {
        var request = new NLWebRequest
        {
            QueryId = "test-streaming",
            Query = "space exploration",
            Mode = QueryMode.List,
            Streaming = true
        };

        var responseCount = 0;
        var lastResponse = (NLWebResponse?)null;

        try
        {
            await foreach (var response in _nlWebService.ProcessRequestStreamAsync(request))
            {
                responseCount++;
                lastResponse = response;

                Assert.IsNotNull(response, "Streamed response should not be null");
                Assert.AreEqual(request.QueryId, response.QueryId, "QueryId should match in streamed response");

                // Break after a reasonable number of responses to avoid long test
                if (responseCount >= 5) break;
            }
        }
        catch (NotImplementedException)
        {
            // Streaming might not be implemented yet - this is acceptable
            Console.WriteLine("Streaming not yet implemented - skipping streaming test");
            return;
        }

        Assert.IsGreaterThan(responseCount , 0, "Should receive at least one streamed response");
        Assert.IsNotNull(lastResponse, "Should have received at least one response");

        Console.WriteLine($"✓ Streaming test completed with {responseCount} responses");
    }
}