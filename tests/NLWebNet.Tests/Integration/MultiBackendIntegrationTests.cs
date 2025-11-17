using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Integration;

[TestClass]
public class MultiBackendIntegrationTests
{
    [TestMethod]
    public async Task EndToEnd_MultiBackendSearch_WorksCorrectly()
    {
        // Arrange - Set up a complete multi-backend service configuration
        var services = new ServiceCollection();

        services.AddNLWebNetMultiBackend(
            options =>
            {
                options.DefaultMode = QueryMode.List;
                options.MaxResultsPerQuery = 20;
                options.EnableDecontextualization = false; // Simplify for test
            },
            multiBackendOptions =>
            {
                multiBackendOptions.Enabled = true;
                multiBackendOptions.EnableParallelQuerying = true;
                multiBackendOptions.EnableResultDeduplication = true;
                multiBackendOptions.MaxConcurrentQueries = 2;
                multiBackendOptions.BackendTimeoutSeconds = 10;
            });

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();
        var backendManager = serviceProvider.GetRequiredService<IBackendManager>();

        // Act - Perform a search using the NLWebService
        var request = new NLWebRequest
        {
            QueryId = "test-001",
            Query = "millennium falcon",
            Mode = QueryMode.List,
            Site = null
        };

        var response = await nlWebService.ProcessRequestAsync(request);

        // Assert - Verify the response contains results from multiple backends
        Assert.IsNotNull(response);
        Assert.AreEqual("test-001", response.QueryId);
        Assert.IsNull(response.Error, "Response should not have an error");
        Assert.IsNotNull(response.Results);
        Assert.IsNotEmpty(response.Results, "Should return search results");

        // Verify backend manager provides information about backends
        var backendInfo = backendManager.GetBackendInfo().ToList();
        Assert.IsGreaterThanOrEqualTo(backendInfo.Count, 1, "Should have at least one backend configured");
        var hasMatchingItem = backendInfo.Any(b => b.IsWriteEndpoint);
        Assert.IsTrue(hasMatchingItem, "Should have a write endpoint designated");

        // Verify write backend is accessible
        var writeBackend = backendManager.GetWriteBackend();
        Assert.IsNotNull(writeBackend, "Should have a write backend available");

        var capabilities = writeBackend.GetCapabilities();
        Assert.IsNotNull(capabilities, "Write backend should have capabilities");
    }

    [TestMethod]
    public async Task EndToEnd_MultiBackendDisabled_FallsBackToSingleBackend()
    {
        // Arrange - Set up multi-backend service but with multi-backend disabled
        var services = new ServiceCollection();

        services.AddNLWebNetMultiBackend(
            options =>
            {
                options.DefaultMode = QueryMode.List;
                options.MaxResultsPerQuery = 20;
                options.EnableDecontextualization = false;
                options.MultiBackend.Enabled = false; // Disabled for backward compatibility
            });

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        // Act - Perform a search
        var request = new NLWebRequest
        {
            QueryId = "test-002",
            Query = "millennium falcon",
            Mode = QueryMode.List
        };

        var response = await nlWebService.ProcessRequestAsync(request);

        // Assert - Should still work in single-backend mode
        Assert.IsNotNull(response);
        Assert.AreEqual("test-002", response.QueryId);
        Assert.IsNull(response.Error, "Response should not have an error");

        // Verify configuration
        var options = serviceProvider.GetRequiredService<IOptions<NLWebOptions>>();
        Assert.IsFalse(options.Value.MultiBackend.Enabled, "Multi-backend should be disabled");
    }

    [TestMethod]
    public async Task EndToEnd_StreamingResponse_WorksWithMultiBackend()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddNLWebNetMultiBackend(options =>
        {
            options.EnableStreaming = true;
            options.MultiBackend.Enabled = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        // Act - Test streaming response
        var request = new NLWebRequest
        {
            QueryId = "test-003",
            Query = "millennium falcon",
            Mode = QueryMode.List,
            Streaming = true
        };

        var responseCount = 0;
        await foreach (var response in nlWebService.ProcessRequestStreamAsync(request))
        {
            responseCount++;
            Assert.IsNotNull(response);
            Assert.AreEqual("test-003", response.QueryId);

            // Break after a few responses to avoid long test
            if (responseCount >= 3) break;
        }

        Assert.IsGreaterThan(responseCount, 0, "Should receive streaming responses");
    }

    [TestMethod]
    public async Task EndToEnd_DeduplicationAcrossBackends_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddNLWebNetMultiBackend(options =>
        {
            options.MultiBackend.Enabled = true;
            options.MultiBackend.EnableResultDeduplication = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var backendManager = serviceProvider.GetRequiredService<IBackendManager>();

        // Act - Direct test of backend manager deduplication
        var results = await backendManager.SearchAsync("millennium falcon", maxResults: 20);

        // Assert
        var resultList = results.ToList();
        var uniqueUrls = resultList.Select(r => r.Url).Distinct().Count();

        Assert.AreEqual(resultList.Count, uniqueUrls,
            "Results should be deduplicated - no duplicate URLs");

        if (resultList.Count > 1)
        {
            // Verify results are sorted by score
            var scores = resultList.Select(r => r.Score).ToList();
            var sortedScores = scores.OrderByDescending(s => s).ToList();
            CollectionAssert.AreEqual(sortedScores, scores,
                "Results should be sorted by relevance score");
        }
    }

    /// <summary>
    /// Tests multi-backend query consistency using comprehensive test scenarios
    /// </summary>
    [TestMethod]
    public async Task EndToEnd_MultiBackendConsistency_ResultsAreConsistent()
    {
        // Test with multi-backend enabled
        var services = new ServiceCollection();
        services.AddNLWebNetMultiBackend(
            options =>
            {
                options.DefaultMode = QueryMode.List;
                options.MaxResultsPerQuery = 10;
            },
            multiBackendOptions =>
            {
                multiBackendOptions.Enabled = true;
                multiBackendOptions.EnableParallelQuerying = true;
                multiBackendOptions.EnableResultDeduplication = true;
            });

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        var consistencyScenarios = TestData.TestDataManager.GetConsistencyScenarios();

        foreach (var scenario in consistencyScenarios)
        {
            Console.WriteLine($"Testing consistency for: {scenario.Name}");

            var request = new NLWebRequest
            {
                QueryId = $"consistency-{Guid.NewGuid():N}",
                Query = scenario.Query,
                Mode = QueryMode.List
            };

            // Run the same query multiple times
            var responses = new List<NLWebResponse>();
            for (int i = 0; i < 3; i++)
            {
                var response = await nlWebService.ProcessRequestAsync(request);
                responses.Add(response);
            }

            // Verify all responses are successful
            foreach (var response in responses)
            {
                Assert.IsNotNull(response, $"Response should not be null for scenario: {scenario.Name}");
                Assert.IsNull(response.Error, $"Response should not have error for scenario: {scenario.Name}");
            }

            // Analyze result consistency
            if (responses.All(r => r.Results?.Any() == true))
            {
                var firstResults = responses[0].Results!.ToList();

                foreach (var response in responses.Skip(1))
                {
                    var currentResults = response.Results!.ToList();

                    // Check for reasonable overlap in results
                    var commonUrls = firstResults.Select(r => r.Url)
                        .Intersect(currentResults.Select(r => r.Url))
                        .Count();

                    var overlapPercent = (double)commonUrls / Math.Max(firstResults.Count, currentResults.Count) * 100;

                    Console.WriteLine($"Result overlap: {overlapPercent:F1}% ({commonUrls} common URLs)");

                    // Results should have reasonable consistency for the same query
                    // Note: Some variation is expected due to scoring differences or backend variations
                    Assert.IsLessThanOrEqualTo(overlapPercent >= scenario.MinOverlapPercent || firstResults.Count , 2,
                        $"Results should have at least {scenario.MinOverlapPercent}% overlap for consistent queries. " +
                        $"Got {overlapPercent:F1}% for scenario: {scenario.Name}");
                }
            }

            Console.WriteLine($"✓ Consistency validated for '{scenario.Name}'");
        }
    }

    /// <summary>
    /// Tests backend capability verification across multiple backends
    /// </summary>
    [TestMethod]
    public Task EndToEnd_BackendCapabilities_AreAccessibleAndValid()
    {
        var services = new ServiceCollection();
        services.AddNLWebNetMultiBackend(options =>
        {
            options.MultiBackend.Enabled = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var backendManager = serviceProvider.GetRequiredService<IBackendManager>();

        var backendInfo = backendManager.GetBackendInfo().ToList();

        Assert.IsGreaterThanOrEqualTo(backendInfo.Count, 1, "Should have at least one backend configured");

        foreach (var backend in backendInfo)
        {
            Console.WriteLine($"Testing backend capabilities: {backend.Id}");

            // Verify backend information is complete
            Assert.IsFalse(string.IsNullOrWhiteSpace(backend.Id), "Backend ID should not be empty");
            Assert.IsNotNull(backend.Capabilities, "Backend capabilities should not be null");
            Assert.IsFalse(string.IsNullOrWhiteSpace(backend.Capabilities.Description), "Backend description should not be empty");

            // Test backend capabilities
            if (backend.IsWriteEndpoint)
            {
                var writeBackend = backendManager.GetWriteBackend();
                Assert.IsNotNull(writeBackend, "Write backend should be accessible");

                var capabilities = writeBackend.GetCapabilities();
                Assert.IsNotNull(capabilities, "Backend capabilities should not be null");
                Assert.IsFalse(string.IsNullOrWhiteSpace(capabilities.Description),
                    "Backend capabilities description should not be empty");

                Console.WriteLine($"✓ Write backend capabilities verified: {capabilities.Description}");
            }

            Console.WriteLine($"✓ Backend '{backend.Id}' capabilities validated");
        }

        return Task.CompletedTask;
    }
}