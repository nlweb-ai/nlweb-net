using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

[TestClass]
public class ToolSelectorTests
{
    private ToolSelector _toolSelector = null!;
    private ILogger<ToolSelector> _logger = null!;
    private IOptions<NLWebOptions> _options = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new TestLogger<ToolSelector>();
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = true };
        _options = Options.Create(nlWebOptions);
        _toolSelector = new ToolSelector(_logger, _options);
    }

    [TestMethod]
    public void ShouldSelectTool_WhenToolSelectionDisabled_ReturnsFalse()
    {
        // Arrange
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = false };
        var options = Options.Create(nlWebOptions);
        var toolSelector = new ToolSelector(_logger, options);
        var request = new NLWebRequest
        {
            Query = "search for something",
            Mode = QueryMode.List
        };

        // Act
        var result = toolSelector.ShouldSelectTool(request);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldSelectTool_WhenGenerateMode_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "generate something",
            Mode = QueryMode.Generate
        };

        // Act
        var result = _toolSelector.ShouldSelectTool(request);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldSelectTool_WhenDecontextualizedQueryExists_ReturnsFalse()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for something",
            Mode = QueryMode.List,
            DecontextualizedQuery = "already processed query"
        };

        // Act
        var result = _toolSelector.ShouldSelectTool(request);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldSelectTool_WhenValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for something",
            Mode = QueryMode.List
        };

        // Act
        var result = _toolSelector.ShouldSelectTool(request);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenShouldNotSelectTool_ReturnsNull()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "generate something",
            Mode = QueryMode.Generate
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenSearchKeywords_ReturnsSearchTool()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for information about APIs",
            Mode = QueryMode.List
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.AreEqual("search", result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenCompareKeywords_ReturnsCompareTool()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "compare these two options",
            Mode = QueryMode.List
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.AreEqual("compare", result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenDetailsKeywords_ReturnsDetailsTool()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "tell me about this feature",
            Mode = QueryMode.List
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.AreEqual("details", result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenEnsembleKeywords_ReturnsEnsembleTool()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "recommend a set of tools for development",
            Mode = QueryMode.List
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.AreEqual("ensemble", result);
    }

    [TestMethod]
    public async Task SelectToolAsync_WhenGeneralQuery_ReturnsSearchTool()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "what is the weather like",
            Mode = QueryMode.List
        };

        // Act
        var result = await _toolSelector.SelectToolAsync(request);

        // Assert
        Assert.AreEqual("search", result);
    }
}