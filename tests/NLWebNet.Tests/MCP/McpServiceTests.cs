using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NLWebNet.MCP;
using NLWebNet.Models;
using NLWebNet.Services;
using System.Threading;
using System.Threading.Tasks;

namespace NLWebNet.Tests.MCP;

[TestClass]
public class McpServiceTests
{
    private INLWebService _mockNLWebService = null!;
    private ILogger<McpService> _mockLogger = null!;
    private McpService _mcpService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockNLWebService = Substitute.For<INLWebService>();
        _mockLogger = Substitute.For<ILogger<McpService>>();
        _mcpService = new McpService(_mockNLWebService, _mockLogger);
    }

    [TestMethod]
    public async Task ListToolsAsync_ReturnsExpectedTools()
    {
        // Act
        var result = await _mcpService.ListToolsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Tools);
        Assert.HasCount(2, result.Tools);

        var searchTool = result.Tools.Find(t => t.Name == "nlweb_search");
        Assert.IsNotNull(searchTool);
        Assert.AreEqual("Search for information using natural language queries with support for different modes (list, summarize, generate)", searchTool.Description);

        var historyTool = result.Tools.Find(t => t.Name == "nlweb_query_history");
        Assert.IsNotNull(historyTool);
        Assert.AreEqual("Search using conversation history for contextual queries", historyTool.Description);
    }

    [TestMethod]
    public async Task ListPromptsAsync_ReturnsExpectedPrompts()
    {
        // Act
        var result = await _mcpService.ListPromptsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Prompts);
        Assert.HasCount(3, result.Prompts);

        var searchPrompt = result.Prompts.Find(p => p.Name == "nlweb_search_prompt");
        Assert.IsNotNull(searchPrompt);
        Assert.AreEqual("Generate a well-structured search query for NLWeb", searchPrompt.Description);

        var summarizePrompt = result.Prompts.Find(p => p.Name == "nlweb_summarize_prompt");
        Assert.IsNotNull(summarizePrompt);

        var generatePrompt = result.Prompts.Find(p => p.Name == "nlweb_generate_prompt");
        Assert.IsNotNull(generatePrompt);
    }

    [TestMethod]
    public async Task CallToolAsync_NLWebSearch_ReturnsSuccessResult()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "nlweb_search",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "test query",
                ["mode"] = "list"
            }
        };

        var expectedResponse = new NLWebResponse
        {
            QueryId = "test-123",
            Results = new List<NLWebResult>
            {
                new()
                {
                    Name = "Test Result",
                    Url = "https://example.com",
                    Score = 0.95,
                    Description = "A test result"
                }
            }
        };

        _mockNLWebService
            .ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _mcpService.CallToolAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsError);
        Assert.HasCount(1, result.Content);
        Assert.AreEqual("text", result.Content[0].Type);
        Assert.Contains("test-123", result.Content[0].Text);
        Assert.Contains("Test Result", result.Content[0].Text);

        await _mockNLWebService.Received(1).ProcessRequestAsync(
            Arg.Is<NLWebRequest>(r => r.Query == "test query" && r.Mode == QueryMode.List),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CallToolAsync_NLWebSearchWithHistory_ProcessesPreviousQueries()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "nlweb_query_history",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "follow up question",
                ["previous_queries"] = new[] { "first question", "second question" },
                ["mode"] = "summarize"
            }
        };

        var expectedResponse = new NLWebResponse
        {
            QueryId = "history-456",
            Summary = "Summary of results",
            Results = new List<NLWebResult>()
        };

        _mockNLWebService
            .ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _mcpService.CallToolAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsError);

        await _mockNLWebService.Received(1).ProcessRequestAsync(
            Arg.Is<NLWebRequest>(r =>
                r.Query == "follow up question" &&
                r.Mode == QueryMode.Summarize &&
                r.Prev == "first question,second question"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CallToolAsync_UnknownTool_ReturnsError()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "unknown_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = await _mcpService.CallToolAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsError);
        Assert.HasCount(1, result.Content);
        Assert.Contains("Unknown tool: unknown_tool", result.Content[0].Text);
    }

    [TestMethod]
    public async Task CallToolAsync_MissingQuery_ReturnsError()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "nlweb_search",
            Arguments = new Dictionary<string, object>() // No query parameter
        };

        // Act
        var result = await _mcpService.CallToolAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsError);
        Assert.HasCount(1, result.Content);
        Assert.Contains("Query parameter is required", result.Content[0].Text);
    }

    [TestMethod]
    public async Task GetPromptAsync_SearchPrompt_ReturnsValidPrompt()
    {
        // Arrange
        var request = new McpGetPromptRequest
        {
            Name = "nlweb_search_prompt",
            Arguments = new Dictionary<string, object>
            {
                ["topic"] = "artificial intelligence",
                ["context"] = "machine learning applications"
            }
        };

        // Act
        var result = await _mcpService.GetPromptAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Structured search prompt for NLWeb", result.Description);
        Assert.HasCount(1, result.Messages);
        Assert.AreEqual("user", result.Messages[0].Role);
        Assert.Contains("artificial intelligence", result.Messages[0].Content.Text);
        Assert.Contains("machine learning applications", result.Messages[0].Content.Text);
    }

    [TestMethod]
    public async Task GetPromptAsync_SummarizePrompt_ReturnsValidPrompt()
    {
        // Arrange
        var request = new McpGetPromptRequest
        {
            Name = "nlweb_summarize_prompt",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "test search query",
                ["result_count"] = "5"
            }
        };

        // Act
        var result = await _mcpService.GetPromptAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Prompt for summarizing NLWeb search results", result.Description);
        Assert.HasCount(2, result.Messages);
        Assert.AreEqual("system", result.Messages[0].Role);
        Assert.AreEqual("user", result.Messages[1].Role);
        Assert.Contains("test search query", result.Messages[1].Content.Text);
    }

    [TestMethod]
    public async Task GetPromptAsync_UnknownPrompt_ReturnsError()
    {
        // Arrange
        var request = new McpGetPromptRequest
        {
            Name = "unknown_prompt",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = await _mcpService.GetPromptAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Unknown prompt: unknown_prompt", result.Description);
        Assert.HasCount(1, result.Messages);
        Assert.AreEqual("system", result.Messages[0].Role);
        Assert.Contains("Error: Unknown prompt 'unknown_prompt'", result.Messages[0].Content.Text);
    }

    [TestMethod]
    public async Task ProcessNLWebQueryAsync_CallsUnderlyingService()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.Generate,
            Site = "test-site"
        };

        var expectedResponse = new NLWebResponse
        {
            QueryId = "direct-789",
            Results = new List<NLWebResult>()
        };

        _mockNLWebService
            .ProcessRequestAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _mcpService.ProcessNLWebQueryAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("direct-789", result.QueryId);

        await _mockNLWebService.Received(1).ProcessRequestAsync(request, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CallToolAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _mcpService.CallToolAsync(null!));
    }

    [TestMethod]
    public async Task GetPromptAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _mcpService.GetPromptAsync(null!));
    }

    [TestMethod]
    public async Task ProcessNLWebQueryAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _mcpService.ProcessNLWebQueryAsync(null!));
    }
}
