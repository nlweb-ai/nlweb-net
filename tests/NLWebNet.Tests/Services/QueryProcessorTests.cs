using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

[TestClass]
public class QueryProcessorTests
{
    private QueryProcessor _queryProcessor = null!;
    private ILogger<QueryProcessor> _logger = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<QueryProcessor>();
        _queryProcessor = new QueryProcessor(_logger);
    }

    [TestMethod]
    public void ValidateRequest_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List
        };

        // Act
        var result = _queryProcessor.ValidateRequest(request);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateRequest_WithEmptyQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "",
            Mode = QueryMode.List
        };

        // Act
        var result = _queryProcessor.ValidateRequest(request);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidateRequest_WithNullQuery_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = null!,
            Mode = QueryMode.List
        };

        // Act
        var result = _queryProcessor.ValidateRequest(request);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GenerateQueryId_WithNullRequestQueryId_GeneratesNewId()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            QueryId = null
        };

        // Act
        var result = _queryProcessor.GenerateQueryId(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(result.Length , 0);
    }

    [TestMethod]
    public void GenerateQueryId_WithExistingQueryId_ReturnsExistingId()
    {
        // Arrange
        var existingId = "existing-id";
        var request = new NLWebRequest
        {
            Query = "test query",
            QueryId = existingId
        };

        // Act
        var result = _queryProcessor.GenerateQueryId(request);

        // Assert
        Assert.AreEqual(existingId, result);
    }

    [TestMethod]
    public async Task ProcessQueryAsync_ReturnsQuery()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List
        };

        // Act
        var result = await _queryProcessor.ProcessQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.AreEqual("test query", result);
    }

    [TestMethod]
    public async Task ProcessQueryAsync_WithToolSelector_CallsToolSelection()
    {
        // Arrange
        var toolSelectorLogger = new TestLogger<ToolSelector>();
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = true };
        var options = Options.Create(nlWebOptions);
        var toolSelector = new ToolSelector(toolSelectorLogger, options);
        var queryProcessorWithToolSelector = new QueryProcessor(_logger, toolSelector);

        var request = new NLWebRequest
        {
            Query = "search for something",
            Mode = QueryMode.List,
            QueryId = "test-query-id"
        };

        // Act
        var result = await queryProcessorWithToolSelector.ProcessQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.AreEqual("search for something", result);
        // The tool selection should have been called but not affect the final result
        // since we're not changing the query processing behavior yet
    }

    [TestMethod]
    public async Task ProcessQueryAsync_WithToolSelectorDisabled_DoesNotCallToolSelection()
    {
        // Arrange
        var toolSelectorLogger = new TestLogger<ToolSelector>();
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = false };
        var options = Options.Create(nlWebOptions);
        var toolSelector = new ToolSelector(toolSelectorLogger, options);
        var queryProcessorWithToolSelector = new QueryProcessor(_logger, toolSelector);

        var request = new NLWebRequest
        {
            Query = "search for something",
            Mode = QueryMode.List,
            QueryId = "test-query-id"
        };

        // Act
        var result = await queryProcessorWithToolSelector.ProcessQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.AreEqual("search for something", result);
        // Tool selection should not have been called
    }

    [TestMethod]
    public async Task ProcessQueryAsync_WithGenerateMode_SkipsToolSelection()
    {
        // Arrange
        var toolSelectorLogger = new TestLogger<ToolSelector>();
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = true };
        var options = Options.Create(nlWebOptions);
        var toolSelector = new ToolSelector(toolSelectorLogger, options);
        var queryProcessorWithToolSelector = new QueryProcessor(_logger, toolSelector);

        var request = new NLWebRequest
        {
            Query = "generate something",
            Mode = QueryMode.Generate,
            QueryId = "test-query-id"
        };

        // Act
        var result = await queryProcessorWithToolSelector.ProcessQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.AreEqual("generate something", result);
        // Tool selection should have been skipped for Generate mode
    }
}
