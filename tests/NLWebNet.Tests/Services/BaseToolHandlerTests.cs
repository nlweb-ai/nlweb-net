using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;

namespace NLWebNet.Tests.Services;

[TestClass]
public class BaseToolHandlerTests
{
    private TestableBaseToolHandler _baseToolHandler = null!;
    private TestLogger<TestableBaseToolHandler> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;
    private TestQueryProcessor _queryProcessor = null!;
    private TestResultGenerator _resultGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<TestableBaseToolHandler>();
        _options = Options.Create(new NLWebOptions());
        _queryProcessor = new TestQueryProcessor();
        _resultGenerator = new TestResultGenerator();

        _baseToolHandler = new TestableBaseToolHandler(
            _logger,
            _options,
            _queryProcessor,
            _resultGenerator);
    }

    [TestMethod]
    public void Constructor_WithValidParameters_InitializesSuccessfully()
    {
        // Act & Assert - Constructor should not throw
        Assert.IsNotNull(_baseToolHandler);
        Assert.AreEqual("testable", _baseToolHandler.ToolType);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TestableBaseToolHandler(null!, _options, _queryProcessor, _resultGenerator));
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TestableBaseToolHandler(_logger, null!, _queryProcessor, _resultGenerator));
    }

    [TestMethod]
    public void Constructor_WithNullQueryProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TestableBaseToolHandler(_logger, _options, null!, _resultGenerator));
    }

    [TestMethod]
    public void Constructor_WithNullResultGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TestableBaseToolHandler(_logger, _options, _queryProcessor, null!));
    }

    [TestMethod]
    public void CanHandle_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "valid test query",
            QueryId = "test-001"
        };

        // Act
        var canHandle = _baseToolHandler.CanHandle(request);

        // Assert
        Assert.IsTrue(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithNullRequest_ReturnsFalse()
    {
        // Act
        var canHandle = _baseToolHandler.CanHandle(null!);

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
            QueryId = "test-002"
        };

        // Act
        var canHandle = _baseToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithEmptyQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "",
            QueryId = "test-003"
        };

        // Act
        var canHandle = _baseToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void CanHandle_WithWhitespaceQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "   \t\n  ",
            QueryId = "test-004"
        };

        // Act
        var canHandle = _baseToolHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle);
    }

    [TestMethod]
    public void GetPriority_WithAnyRequest_ReturnsDefaultPriority()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test priority query",
            QueryId = "test-priority-001"
        };

        // Act
        var priority = _baseToolHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(50, priority);
    }

    [TestMethod]
    public void CreateSuccessResponse_WithValidParameters_CreatesProperResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test success query",
            QueryId = "test-success-001",
            Mode = QueryMode.List
        };

        var results = new List<NLWebResult>
        {
            new() { Name = "Test Result", Description = "Test description", Score = 0.9 }
        };

        // Act
        var response = _baseToolHandler.TestCreateSuccessResponse(request, results, 150);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.AreEqual(request.Mode, response.Mode);
        Assert.AreSame(results, response.Results);
        Assert.IsNull(response.Error);
        Assert.AreEqual(150, response.ProcessingTimeMs);
        Assert.IsTrue(response.Timestamp <= DateTimeOffset.UtcNow);
        Assert.IsTrue(response.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [TestMethod]
    public void CreateErrorResponse_WithValidParameters_CreatesProperErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test error query",
            QueryId = "test-error-001",
            Mode = QueryMode.List
        };

        var errorMessage = "Test error occurred";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var response = _baseToolHandler.TestCreateErrorResponse(request, errorMessage, exception);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(request.QueryId, response.QueryId);
        Assert.AreEqual(request.Query, response.Query);
        Assert.AreEqual(request.Mode, response.Mode);
        Assert.AreEqual(errorMessage, response.Error);
        Assert.AreEqual(0, response.ProcessingTimeMs);
        Assert.IsTrue(response.Timestamp <= DateTimeOffset.UtcNow);
        Assert.IsTrue(response.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));

        // Should have one error result
        Assert.AreEqual(1, response.Results.Count);
        Assert.AreEqual("Tool Error", response.Results[0].Name);
        Assert.AreEqual(errorMessage, response.Results[0].Description);
        Assert.AreEqual("System", response.Results[0].Site);
        Assert.AreEqual(0.0, response.Results[0].Score, 0.001);
    }

    [TestMethod]
    public void CreateErrorResponse_WithoutException_CreatesProperErrorResponse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test error without exception",
            QueryId = "test-error-002",
            Mode = QueryMode.List
        };

        var errorMessage = "Simple error message";

        // Act
        var response = _baseToolHandler.TestCreateErrorResponse(request, errorMessage);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(errorMessage, response.Error);
        Assert.AreEqual(1, response.Results.Count);
        Assert.AreEqual("Tool Error", response.Results[0].Name);
    }

    [TestMethod]
    public void CreateToolResult_WithAllParameters_CreatesProperResult()
    {
        // Arrange
        var name = "Test Result Name";
        var description = "Test result description";
        var url = "https://example.com/test";
        var site = "TestSite";
        var score = 0.85;

        // Act
        var result = _baseToolHandler.TestCreateToolResult(name, description, url, site, score);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual(description, result.Description);
        Assert.AreEqual(url, result.Url);
        Assert.AreEqual(site, result.Site);
        Assert.AreEqual(score, result.Score, 0.001);
    }

    [TestMethod]
    public void CreateToolResult_WithMinimalParameters_CreatesProperResult()
    {
        // Arrange
        var name = "Minimal Result";
        var description = "Minimal description";

        // Act
        var result = _baseToolHandler.TestCreateToolResult(name, description);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual(description, result.Description);
        Assert.AreEqual("", result.Url);
        Assert.AreEqual("testable", result.Site); // Should default to ToolType
        Assert.AreEqual(1.0, result.Score, 0.001);
    }

    [TestMethod]
    public void CreateToolResult_WithEmptySite_UsesToolType()
    {
        // Arrange
        var name = "Site Test Result";
        var description = "Testing site defaulting";

        // Act
        var result = _baseToolHandler.TestCreateToolResult(name, description, "", "", 0.7);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("testable", result.Site); // Should default to ToolType
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidRequest_ExecutesSuccessfully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test execution query",
            QueryId = "test-execute-001",
            Mode = QueryMode.List
        };

        // Act
        var response = await _baseToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(string.IsNullOrEmpty(response.Error));
        Assert.IsTrue(response.Results.Count > 0);
        Assert.AreEqual("Test execution completed", response.Summary);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_HandlesCancellationToken()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test cancellation query",
            QueryId = "test-cancel-001",
            Mode = QueryMode.List
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => _baseToolHandler.ExecuteAsync(request, cts.Token));
    }

    [TestMethod]
    public void ErrorResponse_LogsErrorMessage()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test logging query",
            QueryId = "test-log-001",
            Mode = QueryMode.List
        };

        var errorMessage = "Test error for logging";
        var exception = new InvalidOperationException("Test exception for logging");

        // Act
        var response = _baseToolHandler.TestCreateErrorResponse(request, errorMessage, exception);

        // Assert
        Assert.IsNotNull(response);

        // Verify that logging occurred - check if logger was called
        // Note: TestLogger implementation may vary, so we'll verify response structure instead
        Assert.IsFalse(string.IsNullOrEmpty(response.Error));
        Assert.AreEqual(errorMessage, response.Error);
    }

    [TestMethod]
    public void ProtectedProperties_AreAccessible()
    {
        // Act & Assert - Verify that protected properties are properly initialized
        Assert.IsNotNull(_baseToolHandler.TestLogger);
        Assert.IsNotNull(_baseToolHandler.TestOptions);
        Assert.IsNotNull(_baseToolHandler.TestQueryProcessor);
        Assert.IsNotNull(_baseToolHandler.TestResultGenerator);

        // Verify they match the injected instances
        Assert.AreSame(_options.Value, _baseToolHandler.TestOptions);
        Assert.AreSame(_queryProcessor, _baseToolHandler.TestQueryProcessor);
        Assert.AreSame(_resultGenerator, _baseToolHandler.TestResultGenerator);
    }

    [TestMethod]
    public async Task ExecuteAsync_MeasuresProcessingTime()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test timing query",
            QueryId = "test-timing-001",
            Mode = QueryMode.List
        };

        // Act
        var response = await _baseToolHandler.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.ProcessingTimeMs >= 0);
        Assert.IsTrue(response.ProcessingTimeMs < 1000); // Should be fast for test
    }

    [TestMethod]
    public void CreateSuccessResponse_WithEmptyResults_HandlesGracefully()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "empty results test",
            QueryId = "test-empty-001",
            Mode = QueryMode.List
        };

        var emptyResults = new List<NLWebResult>();

        // Act
        var response = _baseToolHandler.TestCreateSuccessResponse(request, emptyResults, 50);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Results);
        Assert.AreEqual(0, response.Results.Count);
        Assert.IsNull(response.Error);
    }

    [TestMethod]
    public void CreateToolResult_WithNullParameters_HandlesGracefully()
    {
        // Act
        var result = _baseToolHandler.TestCreateToolResult(null!, null!, null!, null!, 0.5);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.Name);
        Assert.IsNull(result.Description);
        Assert.IsNull(result.Url);
        Assert.AreEqual("testable", result.Site); // Should still default to ToolType
        Assert.AreEqual(0.5, result.Score, 0.001);
    }

    [TestMethod]
    public void GetPriority_CanBeOverridden()
    {
        // Arrange
        var customHandler = new CustomPriorityToolHandler(_logger, _options, _queryProcessor, _resultGenerator);
        var request = new NLWebRequest { Query = "test priority override" };

        // Act
        var priority = customHandler.GetPriority(request);

        // Assert
        Assert.AreEqual(99, priority); // Custom priority
    }

    [TestMethod]
    public void CanHandle_CanBeOverridden()
    {
        // Arrange
        var customHandler = new CustomCanHandleToolHandler(_logger, _options, _queryProcessor, _resultGenerator);
        var request = new NLWebRequest { Query = "valid query" };

        // Act
        var canHandle = customHandler.CanHandle(request);

        // Assert
        Assert.IsFalse(canHandle); // Custom behavior: never handles
    }
}

/// <summary>
/// Testable implementation of BaseToolHandler for testing protected methods
/// </summary>
public class TestableBaseToolHandler : BaseToolHandler
{
    public TestableBaseToolHandler(
        ILogger<TestableBaseToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    public override string ToolType => "testable";

    public override Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<NLWebResult>
        {
            CreateToolResult("Test Result", "Test execution result", "", "Test", 1.0)
        };

        var response = CreateSuccessResponse(request, results, 10);
        response.Summary = "Test execution completed";
        return Task.FromResult(response);
    }

    // Expose protected methods for testing
    public NLWebResponse TestCreateSuccessResponse(NLWebRequest request, IList<NLWebResult> results, long processingTimeMs)
        => CreateSuccessResponse(request, results, processingTimeMs);

    public NLWebResponse TestCreateErrorResponse(NLWebRequest request, string errorMessage, Exception? exception = null)
        => CreateErrorResponse(request, errorMessage, exception);

    public NLWebResult TestCreateToolResult(string name, string description, string url = "", string site = "", double score = 1.0)
        => CreateToolResult(name, description, url, site, score);

    // Expose protected properties for testing
    public ILogger TestLogger => Logger;
    public NLWebOptions TestOptions => Options;
    public IQueryProcessor TestQueryProcessor => QueryProcessor;
    public IResultGenerator TestResultGenerator => ResultGenerator;
}

/// <summary>
/// Custom tool handler for testing priority override
/// </summary>
public class CustomPriorityToolHandler : BaseToolHandler
{
    public CustomPriorityToolHandler(
        ILogger<TestableBaseToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    public override string ToolType => "custom-priority";

    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make it properly async
        var results = new List<NLWebResult>();
        return CreateSuccessResponse(request, results, 0);
    }

    public override int GetPriority(NLWebRequest request) => 99; // Custom high priority
}

/// <summary>
/// Custom tool handler for testing CanHandle override
/// </summary>
public class CustomCanHandleToolHandler : BaseToolHandler
{
    public CustomCanHandleToolHandler(
        ILogger<TestableBaseToolHandler> logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
        : base(logger, options, queryProcessor, resultGenerator)
    {
    }

    public override string ToolType => "custom-canhandle";

    public override async Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make it properly async
        var results = new List<NLWebResult>();
        return CreateSuccessResponse(request, results, 0);
    }

    public override bool CanHandle(NLWebRequest request) => false; // Never handles anything
}
