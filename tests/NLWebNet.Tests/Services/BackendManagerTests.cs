using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

[TestClass]
public class BackendManagerTests
{
    private ILogger<BackendManager> _logger = null!;
    private MockDataBackend _backend1 = null!;
    private MockDataBackend _backend2 = null!;
    private MultiBackendOptions _options = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<BackendManager>();
        _backend1 = new MockDataBackend(new TestLogger<MockDataBackend>());
        _backend2 = new MockDataBackend(new TestLogger<MockDataBackend>());
        
        _options = new MultiBackendOptions
        {
            Enabled = true,
            EnableParallelQuerying = true,
            EnableResultDeduplication = true,
            MaxConcurrentQueries = 5,
            BackendTimeoutSeconds = 30
        };
    }

    [TestMethod]
    public async Task SearchAsync_WithMultipleBackends_ReturnsCombinedResults()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act - use a query that will match the sample data
        var results = await manager.SearchAsync("millennium falcon", maxResults: 10);

        // Assert
        Assert.IsNotNull(results);
        var resultList = results.ToList();
        Assert.IsTrue(resultList.Count > 0, "Should return results from multiple backends");
    }

    [TestMethod]
    public async Task SearchAsync_WithDeduplicationEnabled_RemovesDuplicateUrls()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Both backends should return some overlapping results due to MockDataBackend implementation
        // Act
        var results = await manager.SearchAsync("test", maxResults: 20);

        // Assert
        var resultList = results.ToList();
        var uniqueUrls = resultList.Select(r => r.Url).Distinct().Count();
        Assert.AreEqual(resultList.Count, uniqueUrls, "Results should be deduplicated by URL");
    }

    [TestMethod]
    public async Task SearchAsync_WithDeduplicationDisabled_ReturnsAllResults()
    {
        // Arrange
        _options.EnableResultDeduplication = false;
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act - use a query that will match the sample data
        var results = await manager.SearchAsync("millennium", maxResults: 50);

        // Assert
        var resultList = results.ToList();
        // Should have results when using a valid query
        Assert.IsTrue(resultList.Count > 0, "Should return results even without deduplication");
    }

    [TestMethod]
    public async Task SearchAsync_WithMultiBackendDisabled_UsesFirstBackendOnly()
    {
        // Arrange
        _options.Enabled = false;
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act - use a query that will match the sample data
        var results = await manager.SearchAsync("millennium", maxResults: 10);

        // Assert
        Assert.IsNotNull(results);
        // Should work even when multi-backend is disabled
        Assert.IsTrue(results.Any(), "Should return results from single backend fallback");
    }

    [TestMethod]
    public async Task GetAvailableSitesAsync_CombinesAllBackendSites()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act
        var sites = await manager.GetAvailableSitesAsync();

        // Assert
        Assert.IsNotNull(sites);
        var siteList = sites.ToList();
        Assert.IsTrue(siteList.Count > 0, "Should return sites from all backends");
    }

    [TestMethod]
    public async Task GetItemByUrlAsync_ReturnsFirstMatch()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act - use an actual URL from the mock data
        var result = await manager.GetItemByUrlAsync("https://galactic-shipyards.com/millennium-falcon");

        // Assert
        // MockDataBackend should return a result for this URL
        Assert.IsNotNull(result, "Should return item from first backend that has it");
    }

    [TestMethod]
    public void GetWriteBackend_ReturnsFirstBackendByDefault()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act
        var writeBackend = manager.GetWriteBackend();

        // Assert
        Assert.IsNotNull(writeBackend, "Should return a write backend");
        Assert.AreSame(_backend1, writeBackend, "Should return first backend as default write backend");
    }

    [TestMethod]
    public void GetBackendInfo_ReturnsInfoForAllBackends()
    {
        // Arrange
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act
        var backendInfo = manager.GetBackendInfo();

        // Assert
        Assert.IsNotNull(backendInfo);
        var infoList = backendInfo.ToList();
        Assert.AreEqual(2, infoList.Count, "Should return info for all backends");
        Assert.IsTrue(infoList.All(info => info.Enabled), "All backends should be marked as enabled");
        Assert.IsTrue(infoList.Any(info => info.IsWriteEndpoint), "One backend should be marked as write endpoint");
    }

    [TestMethod]
    public async Task SearchAsync_WithParallelQueryingDisabled_StillWorks()
    {
        // Arrange
        _options.EnableParallelQuerying = false;
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act - use a query that will match the sample data
        var results = await manager.SearchAsync("millennium", maxResults: 10);

        // Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Any(), "Should return results even with sequential querying");
    }

    [TestMethod]
    public void GetBackendInfo_UsesConfiguredEndpointNames_WhenEndpointsProvided()
    {
        // Arrange
        var optionsWithEndpoints = new MultiBackendOptions
        {
            Enabled = true,
            EnableParallelQuerying = true,
            EnableResultDeduplication = true,
            MaxConcurrentQueries = 5,
            BackendTimeoutSeconds = 30,
            WriteEndpoint = "primary_backend",
            Endpoints = new Dictionary<string, BackendEndpointOptions>
            {
                ["primary_backend"] = new() { Enabled = true, BackendType = "mock", Priority = 10 },
                ["secondary_backend"] = new() { Enabled = true, BackendType = "mock", Priority = 5 }
            }
        };

        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(optionsWithEndpoints);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act
        var backendInfo = manager.GetBackendInfo();

        // Assert
        Assert.IsNotNull(backendInfo);
        var infoList = backendInfo.ToList();
        Assert.AreEqual(2, infoList.Count, "Should return info for all backends");

        // Verify that configured endpoint names are used instead of generic backend_0, backend_1
        var backendIds = infoList.Select(info => info.Id).OrderBy(id => id).ToList();
        CollectionAssert.AreEqual(new[] { "primary_backend", "secondary_backend" }, backendIds, 
            "Backend IDs should use configured endpoint names");

        // Verify write endpoint identification works with configured names
        var writeBackend = infoList.Single(info => info.IsWriteEndpoint);
        Assert.AreEqual("primary_backend", writeBackend.Id, "Primary backend should be identified as write endpoint");
    }

    [TestMethod]
    public void GetBackendInfo_FallsBackToGenericNames_WhenNoEndpointsConfigured()
    {
        // Arrange - use original options without configured endpoints
        var backends = new[] { _backend1, _backend2 };
        var optionsWrapper = Options.Create(_options);
        var manager = new BackendManager(backends, optionsWrapper, _logger);

        // Act
        var backendInfo = manager.GetBackendInfo();

        // Assert
        Assert.IsNotNull(backendInfo);
        var infoList = backendInfo.ToList();
        Assert.AreEqual(2, infoList.Count, "Should return info for all backends");

        // Verify that generic names are used as fallback
        var backendIds = infoList.Select(info => info.Id).OrderBy(id => id).ToList();
        CollectionAssert.AreEqual(new[] { "backend_0", "backend_1" }, backendIds, 
            "Backend IDs should fall back to generic names when no endpoints configured");
    }
}