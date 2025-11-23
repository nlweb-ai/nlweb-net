using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Extensions;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

[TestClass]
public class ToolExecutorTests
{
    private ServiceProvider _serviceProvider = null!;
    private IToolExecutor _toolExecutor = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Configure NLWebNet options
        services.Configure<NLWebOptions>(options =>
        {
            options.ToolSelectionEnabled = true;
            options.DefaultMode = QueryMode.List;
        });

        // Add NLWebNet services with tool system
        services.AddNLWebNet();

        _serviceProvider = services.BuildServiceProvider();
        _toolExecutor = _serviceProvider.GetRequiredService<IToolExecutor>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public void GetAvailableTools_ShouldReturnAllToolHandlers()
    {
        // Act
        var tools = _toolExecutor.GetAvailableTools().ToList();

        // Assert
        Assert.IsGreaterThan(tools.Count, 0, "Should have at least one tool handler");

        var toolTypes = tools.Select(t => t.ToolType).ToList();
        Assert.Contains("search", toolTypes, "Should include search tool");
        Assert.Contains("details", toolTypes, "Should include details tool");
        Assert.Contains("compare", toolTypes, "Should include compare tool");
        Assert.Contains("ensemble", toolTypes, "Should include ensemble tool");
        Assert.Contains("recipe", toolTypes, "Should include recipe tool");
    }

    [TestMethod]
    public async Task ExecuteToolAsync_WithSearchTool_ShouldReturnResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for information about APIs",
            Mode = QueryMode.List,
            QueryId = "test-search-001"
        };

        // Act
        var response = await _toolExecutor.ExecuteToolAsync(request, "search");

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.IsNotNull(response.Results);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_WithDetailsQuery_ShouldReturnDetailsResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about REST APIs",
            Mode = QueryMode.List,
            QueryId = "test-details-001"
        };

        // Act
        var response = await _toolExecutor.ExecuteToolAsync(request, "details");

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.IsNotNull(response.Results);
        // The mock backend may return empty results, but the response should be processed by details tool
        var containsDetails = response.Summary?.Contains("Details") == true || response.Summary?.Contains("details") == true;
        Assert.IsTrue(containsDetails, "Should be processed by details tool");
    }

    [TestMethod]
    public async Task ExecuteToolAsync_WithInvalidTool_ShouldThrowException()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List,
            QueryId = "test-invalid-001"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _toolExecutor.ExecuteToolAsync(request, "nonexistent-tool"));
    }
}