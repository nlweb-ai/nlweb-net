using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Health;
using NLWebNet.Models;
using NLWebNet.Services;
using NSubstitute;

namespace NLWebNet.Tests.Health;

[TestClass]
public class DataBackendHealthCheckTests
{
    private IDataBackend _mockDataBackend = null!;
    private ILogger<DataBackendHealthCheck> _mockLogger = null!;
    private DataBackendHealthCheck _healthCheck = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockDataBackend = Substitute.For<IDataBackend>();
        _mockLogger = Substitute.For<ILogger<DataBackendHealthCheck>>();
        _healthCheck = new DataBackendHealthCheck(_mockDataBackend, _mockLogger);
    }

    [TestMethod]
    public async Task CheckHealthAsync_BackendResponds_ReturnsHealthy()
    {
        // Arrange
        var context = new HealthCheckContext();
        var mockResults = new List<NLWebResult>
        {
            new NLWebResult { Name = "Test", Url = "http://test.com", Description = "Test result" }
        };
        _mockDataBackend.SearchAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockResults);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        StringAssert.Contains(result.Description!, "is operational");
    }

    [TestMethod]
    public async Task CheckHealthAsync_BackendThrowsNotImplemented_ReturnsHealthyWithLimitedFunctionality()
    {
        // Arrange
        var context = new HealthCheckContext();
        _mockDataBackend.SearchAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<IEnumerable<NLWebResult>>(x => throw new NotImplementedException());

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        StringAssert.Contains(result.Description!, "limited functionality");
    }

    [TestMethod]
    public async Task CheckHealthAsync_BackendThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var context = new HealthCheckContext();
        var exception = new InvalidOperationException("Backend failure");
        _mockDataBackend.SearchAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<IEnumerable<NLWebResult>>(x => throw exception);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        StringAssert.Contains(result.Description!, "Backend failure");
        Assert.AreEqual(exception, result.Exception);
    }

    [TestMethod]
    public void Constructor_NullBackend_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new DataBackendHealthCheck(null!, _mockLogger));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new DataBackendHealthCheck(_mockDataBackend, null!));
    }

    [TestMethod]
    public async Task CheckHealthAsync_ValidOperation_CallsSearchWithHealthCheckQuery()
    {
        // Arrange
        var context = new HealthCheckContext();
        var mockResults = new List<NLWebResult>();
        _mockDataBackend.SearchAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockResults);

        // Act
        await _healthCheck.CheckHealthAsync(context);

        // Assert
        await _mockDataBackend.Received(1).SearchAsync("health-check", cancellationToken: Arg.Any<CancellationToken>());
    }
}