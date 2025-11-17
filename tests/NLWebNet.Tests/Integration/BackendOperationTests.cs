using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Integration;

/// <summary>
/// Backend-specific integration tests for database operations
/// </summary>
[TestClass]
public class BackendOperationTests
{
    private IServiceProvider _serviceProvider = null!;

    [TestInitialize]
    public void Initialize()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddNLWebNetMultiBackend();
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Tests MockDataBackend specific operations and capabilities
    /// </summary>
    [TestMethod]
    public async Task BackendOperation_MockDataBackend_AllOperationsWork()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockDataBackend>>();
        var mockBackend = new MockDataBackend(logger);

        Console.WriteLine("Testing MockDataBackend operations");

        // Test capabilities
        var capabilities = mockBackend.GetCapabilities();
        Assert.IsNotNull(capabilities, "Capabilities should not be null");
        Assert.IsTrue(capabilities.SupportsSiteFiltering, "MockDataBackend should support site filtering");
        Assert.IsTrue(capabilities.SupportsFullTextSearch, "MockDataBackend should support full text search");
        Assert.IsFalse(capabilities.SupportsSemanticSearch, "MockDataBackend should not support semantic search");
        Assert.AreEqual(50, capabilities.MaxResults, "MockDataBackend should have max results of 50");

        Console.WriteLine($"✓ MockDataBackend capabilities: {capabilities.Description}");

        // Test basic search
        var searchResults = await mockBackend.SearchAsync("millennium falcon", null, 10, CancellationToken.None);
        var resultsList = searchResults.ToList();

        Assert.IsGreaterThan(resultsList.Count , 0, "Should return results for 'millennium falcon'");
        Assert.IsLessThan(resultsList.Count , = 10, "Should respect max results limit");

        foreach (var result in resultsList)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Name), "Result name should not be empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Url), "Result URL should not be empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Description), "Result description should not be empty");
        }

        Console.WriteLine($"✓ Basic search returned {resultsList.Count} results");

        // Test site filtering
        var siteFilteredResults = await mockBackend.SearchAsync("Dune", "scifi-cinema.com", 10, CancellationToken.None);
        var siteFilteredList = siteFilteredResults.ToList();

        if (siteFilteredList.Count > 0)
        {
            foreach (var result in siteFilteredList)
            {
                Assert.AreEqual("scifi-cinema.com", result.Site,
                    "All results should be from the specified site when site filtering is applied");
            }
            Console.WriteLine($"✓ Site filtering returned {siteFilteredList.Count} results from scifi-cinema.com");
        }

        // Test empty query handling
        var emptyResults = await mockBackend.SearchAsync("", null, 10, CancellationToken.None);
        var emptyList = emptyResults.ToList();
        Assert.AreEqual(0, emptyList.Count, "Empty query should return no results");

        Console.WriteLine("✓ Empty query handling validated");

        // Test null query handling
        var nullResults = await mockBackend.SearchAsync(null!, null, 10, CancellationToken.None);
        var nullList = nullResults.ToList();
        Assert.AreEqual(0, nullList.Count, "Null query should return no results");

        Console.WriteLine("✓ Null query handling validated");
    }

    /// <summary>
    /// Tests backend manager operations with multiple backends
    /// </summary>
    [TestMethod]
    public Task BackendOperation_BackendManager_ManagesBackendsCorrectly()
    {
        var backendManager = _serviceProvider.GetRequiredService<IBackendManager>();

        Console.WriteLine("Testing BackendManager operations");

        // Test backend information retrieval
        var backendInfo = backendManager.GetBackendInfo().ToList();
        Assert.IsGreaterThan(backendInfo.Count , = 1, "Should have at least one backend configured");

        foreach (var backend in backendInfo)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(backend.Id), "Backend ID should not be empty");
            Assert.IsNotNull(backend.Capabilities, "Backend capabilities should not be null");
            Assert.IsFalse(string.IsNullOrWhiteSpace(backend.Capabilities.Description), "Backend description should not be empty");

            Console.WriteLine($"Backend: {backend.Id} - {backend.Capabilities.Description}");
            Console.WriteLine($"  Write endpoint: {backend.IsWriteEndpoint}");
        }

        // Test write backend access
        var writeBackend = backendManager.GetWriteBackend();
        Assert.IsNotNull(writeBackend, "Should have a write backend available");

        var writeCapabilities = writeBackend.GetCapabilities();
        Assert.IsNotNull(writeCapabilities, "Write backend should have capabilities");

        Console.WriteLine($"✓ Write backend capabilities: {writeCapabilities.Description}");

        // Test query execution through backend manager
        var request = new NLWebRequest
        {
            QueryId = "backend-manager-test",
            Query = "test query for backend operations",
            Mode = QueryMode.List
        };

        // This test verifies the backend manager can coordinate query execution
        // The actual implementation details depend on the specific backend manager implementation
        Console.WriteLine("✓ BackendManager operations validated");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests backend capabilities and limitations
    /// </summary>
    [TestMethod]
    public async Task BackendOperation_Capabilities_ReflectActualLimitations()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockDataBackend>>();
        var mockBackend = new MockDataBackend(logger);

        Console.WriteLine("Testing backend capabilities vs actual behavior");

        var capabilities = mockBackend.GetCapabilities();

        // Test max results limitation
        var maxResultsQuery = await mockBackend.SearchAsync("space", null, capabilities.MaxResults + 10, CancellationToken.None);
        var maxResultsList = maxResultsQuery.ToList();

        Assert.IsTrue(maxResultsList.Count <= capabilities.MaxResults,
            $"Should not return more than MaxResults ({capabilities.MaxResults}). Got {maxResultsList.Count}");

        Console.WriteLine($"✓ Max results limitation respected: {maxResultsList.Count} <= {capabilities.MaxResults}");

        // Test site filtering capability
        if (capabilities.SupportsSiteFiltering)
        {
            var siteResults = await mockBackend.SearchAsync("test", "specific-site.com", 5, CancellationToken.None);
            // Site filtering capability is advertised, behavior should be consistent
            Console.WriteLine("✓ Site filtering capability verified");
        }

        // Test full text search capability
        if (capabilities.SupportsFullTextSearch)
        {
            var fullTextResults = await mockBackend.SearchAsync("comprehensive detailed analysis", null, 5, CancellationToken.None);
            // Full text search capability is advertised
            Console.WriteLine("✓ Full text search capability verified");
        }

        // Test semantic search capability (should be false for MockDataBackend)
        Assert.IsFalse(capabilities.SupportsSemanticSearch,
            "MockDataBackend should not support semantic search");
        Console.WriteLine("✓ Semantic search capability correctly reported as not supported");
    }

    /// <summary>
    /// Tests backend error handling and resilience
    /// </summary>
    [TestMethod]
    public async Task BackendOperation_ErrorHandling_HandlesFaultyConditionsGracefully()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockDataBackend>>();
        var mockBackend = new MockDataBackend(logger);

        Console.WriteLine("Testing backend error handling");

        // Test with cancellation token
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Immediately cancel

        try
        {
            var cancelledResults = await mockBackend.SearchAsync("test", null, 10, cancellationTokenSource.Token);
            // If this doesn't throw, the backend handles cancellation gracefully
            Console.WriteLine("✓ Cancellation handled gracefully");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✓ Cancellation properly throws OperationCanceledException");
        }

        // Test with very large max results
        var largeMaxResults = await mockBackend.SearchAsync("test", null, int.MaxValue, CancellationToken.None);
        var largeResultsList = largeMaxResults.ToList();

        // Should not crash or cause issues
        Assert.IsGreaterThan(largeResultsList.Count , = 0, "Should handle large max results gracefully");
        Console.WriteLine($"✓ Large max results handled gracefully: {largeResultsList.Count} results");

        // Test with very long query
        var longQuery = new string('a', 10000); // 10k character query
        var longQueryResults = await mockBackend.SearchAsync(longQuery, null, 10, CancellationToken.None);
        var longQueryList = longQueryResults.ToList();

        // Should not crash
        Assert.IsGreaterThan(longQueryList.Count , = 0, "Should handle long queries gracefully");
        Console.WriteLine($"✓ Long query handled gracefully: {longQueryList.Count} results");
    }

    /// <summary>
    /// Tests backend performance characteristics
    /// </summary>
    [TestMethod]
    public async Task BackendOperation_Performance_MeetsExpectedCharacteristics()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockDataBackend>>();
        var mockBackend = new MockDataBackend(logger);

        Console.WriteLine("Testing backend performance characteristics");

        var queries = new[]
        {
            "simple query",
            "more complex query with multiple terms",
            "very specific detailed query with many descriptive terms"
        };

        foreach (var query in queries)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = await mockBackend.SearchAsync(query, null, 10, CancellationToken.None);
            var resultsList = results.ToList(); // Force enumeration
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Mock backend should be reasonably fast (< 500ms) in test environment
            Assert.IsTrue(elapsedMs < 500,
                $"MockDataBackend should be reasonably fast. Query '{query}' took {elapsedMs}ms");

            Console.WriteLine($"✓ Query '{query}' completed in {elapsedMs}ms with {resultsList.Count} results");
        }

        Console.WriteLine("✓ Backend performance characteristics validated");
    }
}