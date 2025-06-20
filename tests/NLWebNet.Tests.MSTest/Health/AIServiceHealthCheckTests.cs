using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Health;
using NLWebNet.MCP;
using NLWebNet.Models;
using NSubstitute;

namespace NLWebNet.Tests.Health;

[TestClass]
public class AIServiceHealthCheckTests
{
    private IMcpService _mockMcpService = null!;
    private ILogger<AIServiceHealthCheck> _mockLogger = null!;
    private AIServiceHealthCheck _healthCheck = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMcpService = Substitute.For<IMcpService>();
        _mockLogger = Substitute.For<ILogger<AIServiceHealthCheck>>();
        _healthCheck = new AIServiceHealthCheck(_mockMcpService, _mockLogger);
    }

    [TestMethod]
    public async Task CheckHealthAsync_ServiceResponds_ReturnsHealthy()
    {
        // Arrange
        var context = new HealthCheckContext();
        var mockResponse = new McpListToolsResponse
        {
            Tools = new List<McpTool>
            {
                new McpTool { Name = "test-tool", Description = "Test tool" }
            }
        };
        _mockMcpService.ListToolsAsync(Arg.Any<CancellationToken>()).Returns(mockResponse);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        Assert.AreEqual("AI/MCP service is operational", result.Description);
    }

    [TestMethod]
    public async Task CheckHealthAsync_ServiceReturnsNull_ReturnsDegraded()
    {
        // Arrange
        var context = new HealthCheckContext();
        _mockMcpService.ListToolsAsync(Arg.Any<CancellationToken>()).Returns((McpListToolsResponse?)null);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        StringAssert.Contains(result.Description!, "returned null tools list");
    }

    [TestMethod]
    public async Task CheckHealthAsync_ServiceThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var context = new HealthCheckContext();
        var exception = new InvalidOperationException("Service failure");
        _mockMcpService.ListToolsAsync(Arg.Any<CancellationToken>())
            .Returns<McpListToolsResponse>(x => throw exception);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        StringAssert.Contains(result.Description!, "Service failure");
        Assert.AreEqual(exception, result.Exception);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new AIServiceHealthCheck(null!, _mockLogger));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new AIServiceHealthCheck(_mockMcpService, null!));
    }

    [TestMethod]
    public async Task CheckHealthAsync_ValidOperation_CallsListTools()
    {
        // Arrange
        var context = new HealthCheckContext();
        var mockResponse = new McpListToolsResponse { Tools = new List<McpTool>() };
        _mockMcpService.ListToolsAsync(Arg.Any<CancellationToken>()).Returns(mockResponse);

        // Act
        await _healthCheck.CheckHealthAsync(context);

        // Assert
        await _mockMcpService.Received(1).ListToolsAsync(Arg.Any<CancellationToken>());
    }
}