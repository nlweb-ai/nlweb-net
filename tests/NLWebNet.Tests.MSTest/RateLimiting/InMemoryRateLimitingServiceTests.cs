using Microsoft.Extensions.Options;
using NLWebNet.RateLimiting;

namespace NLWebNet.Tests.RateLimiting;

[TestClass]
public class InMemoryRateLimitingServiceTests
{
    private RateLimitingOptions _options = null!;
    private IOptions<RateLimitingOptions> _optionsWrapper = null!;
    private InMemoryRateLimitingService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _options = new RateLimitingOptions
        {
            Enabled = true,
            RequestsPerWindow = 10,
            WindowSizeInMinutes = 1
        };
        _optionsWrapper = Options.Create(_options);
        _service = new InMemoryRateLimitingService(_optionsWrapper);
    }

    [TestMethod]
    public async Task IsRequestAllowedAsync_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var identifier = "test-client";

        // Act
        var result = await _service.IsRequestAllowedAsync(identifier);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsRequestAllowedAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var identifier = "test-client";

        // Act - Make requests up to the limit
        for (int i = 0; i < _options.RequestsPerWindow; i++)
        {
            await _service.IsRequestAllowedAsync(identifier);
        }

        // Act - Try one more request
        var result = await _service.IsRequestAllowedAsync(identifier);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IsRequestAllowedAsync_DisabledRateLimit_AlwaysReturnsTrue()
    {
        // Arrange
        _options.Enabled = false;
        var identifier = "test-client";

        // Act - Make more requests than the limit
        for (int i = 0; i < _options.RequestsPerWindow + 5; i++)
        {
            var result = await _service.IsRequestAllowedAsync(identifier);
            Assert.IsTrue(result);
        }
    }

    [TestMethod]
    public async Task GetRateLimitStatusAsync_InitialRequest_ReturnsCorrectStatus()
    {
        // Arrange
        var identifier = "test-client";

        // Act
        var status = await _service.GetRateLimitStatusAsync(identifier);

        // Assert
        Assert.IsTrue(status.IsAllowed);
        Assert.AreEqual(_options.RequestsPerWindow, status.RequestsRemaining);
        Assert.AreEqual(0, status.TotalRequests);
    }

    [TestMethod]
    public async Task GetRateLimitStatusAsync_AfterRequests_ReturnsUpdatedStatus()
    {
        // Arrange
        var identifier = "test-client";
        var requestsMade = 3;

        // Act - Make some requests
        for (int i = 0; i < requestsMade; i++)
        {
            await _service.IsRequestAllowedAsync(identifier);
        }

        var status = await _service.GetRateLimitStatusAsync(identifier);

        // Assert
        Assert.IsTrue(status.IsAllowed);
        Assert.AreEqual(_options.RequestsPerWindow - requestsMade, status.RequestsRemaining);
        Assert.AreEqual(requestsMade, status.TotalRequests);
    }

    [TestMethod]
    public async Task GetRateLimitStatusAsync_ExceededLimit_ReturnsNotAllowed()
    {
        // Arrange
        var identifier = "test-client";

        // Act - Exceed the limit
        for (int i = 0; i < _options.RequestsPerWindow + 1; i++)
        {
            await _service.IsRequestAllowedAsync(identifier);
        }

        var status = await _service.GetRateLimitStatusAsync(identifier);

        // Assert
        Assert.IsFalse(status.IsAllowed);
        Assert.AreEqual(0, status.RequestsRemaining);
        Assert.AreEqual(_options.RequestsPerWindow, status.TotalRequests);
    }

    [TestMethod]
    public async Task IsRequestAllowedAsync_DifferentIdentifiers_IndependentLimits()
    {
        // Arrange
        var identifier1 = "client-1";
        var identifier2 = "client-2";

        // Act - Exhaust limit for first client
        for (int i = 0; i < _options.RequestsPerWindow; i++)
        {
            await _service.IsRequestAllowedAsync(identifier1);
        }

        var client1Blocked = await _service.IsRequestAllowedAsync(identifier1);
        var client2Allowed = await _service.IsRequestAllowedAsync(identifier2);

        // Assert
        Assert.IsFalse(client1Blocked);
        Assert.IsTrue(client2Allowed);
    }
}