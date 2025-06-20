using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Health;
using NLWebNet.Services;
using NSubstitute;

namespace NLWebNet.Tests.Health;

[TestClass]
public class NLWebHealthCheckTests
{
    private INLWebService _mockNlWebService = null!;
    private ILogger<NLWebHealthCheck> _mockLogger = null!;
    private NLWebHealthCheck _healthCheck = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockNlWebService = Substitute.For<INLWebService>();
        _mockLogger = Substitute.For<ILogger<NLWebHealthCheck>>();
        _healthCheck = new NLWebHealthCheck(_mockNlWebService, _mockLogger);
    }

    [TestMethod]
    public async Task CheckHealthAsync_ServiceAvailable_ReturnsHealthy()
    {
        // Arrange
        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        Assert.AreEqual("NLWeb service is operational", result.Description);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new NLWebHealthCheck(null!, _mockLogger));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new NLWebHealthCheck(_mockNlWebService, null!));
    }

    [TestMethod]
    public async Task CheckHealthAsync_ValidContext_LogsDebugMessages()
    {
        // Arrange
        var context = new HealthCheckContext();

        // Act
        await _healthCheck.CheckHealthAsync(context);

        // Assert
        _mockLogger.Received().LogDebug("Performing NLWeb service health check");
        _mockLogger.Received().LogDebug("NLWeb service health check completed successfully");
    }
}