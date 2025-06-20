using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NLWebNet.Controllers;
using NLWebNet.MCP;
using NLWebNet.Models;
using Microsoft.AspNetCore.Mvc;

namespace NLWebNet.Tests.Controllers;

[TestClass]
public class McpControllerTests
{
    private IMcpService _mockMcpService = null!;
    private ILogger<McpController> _mockLogger = null!;
    private McpController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMcpService = Substitute.For<IMcpService>();
        _mockLogger = Substitute.For<ILogger<McpController>>();
        _controller = new McpController(_mockMcpService, _mockLogger);
    }

    [TestMethod]
    public async Task ListTools_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new McpListToolsResponse
        {
            Tools = new List<McpTool>
            {
                new()
                {
                    Name = "nlweb_search",
                    Description = "Search tool"
                }
            }
        };

        _mockMcpService.ListToolsAsync().Returns(expectedResponse);

        // Act
        var result = await _controller.ListTools();

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(McpListToolsResponse));
        
        var response = (McpListToolsResponse)okResult.Value!;
        Assert.AreEqual(1, response.Tools?.Count);

        await _mockMcpService.Received(1).ListToolsAsync();
    }

    [TestMethod]
    public async Task ListPrompts_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new McpListPromptsResponse
        {
            Prompts = new List<McpPrompt>
            {
                new()
                {
                    Name = "nlweb_search_prompt",
                    Description = "Search prompt"
                }
            }
        };

        _mockMcpService.ListPromptsAsync().Returns(expectedResponse);

        // Act
        var result = await _controller.ListPrompts();

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(McpListPromptsResponse));
        
        var response = (McpListPromptsResponse)okResult.Value!;
        Assert.AreEqual(1, response.Prompts?.Count);

        await _mockMcpService.Received(1).ListPromptsAsync();
    }

    [TestMethod]
    public async Task CallTool_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "nlweb_search",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "test query"
            }
        };

        var expectedResponse = new McpCallToolResponse
        {
            IsError = false,
            Content = new List<McpContent>
            {
                new()
                {
                    Type = "text",
                    Text = "Tool executed successfully"
                }
            }
        };

        _mockMcpService.CallToolAsync(request).Returns(expectedResponse);

        // Act
        var result = await _controller.CallTool(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(McpCallToolResponse));
        
        var response = (McpCallToolResponse)okResult.Value!;
        Assert.IsFalse(response.IsError);
        Assert.AreEqual(1, response.Content?.Count);

        await _mockMcpService.Received(1).CallToolAsync(request);
    }

    [TestMethod]
    public async Task CallTool_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CallTool(null!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.AreEqual("Invalid Request", problemDetails.Title);
        Assert.AreEqual(400, problemDetails.Status);
    }

    [TestMethod]
    public async Task CallTool_EmptyToolName_ReturnsBadRequest()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = await _controller.CallTool(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.AreEqual("Invalid Tool Name", problemDetails.Title);
        Assert.AreEqual(400, problemDetails.Status);
    }

    [TestMethod]
    public async Task GetPrompt_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new McpGetPromptRequest
        {
            Name = "nlweb_search_prompt",
            Arguments = new Dictionary<string, object>
            {
                ["topic"] = "test topic"
            }
        };        var expectedResponse = new McpGetPromptResponse
        {
            Description = "Search prompt",
            Messages = new List<McpPromptMessage>
            {
                new()
                {
                    Role = "user",
                    Content = new McpContent
                    {
                        Type = "text",
                        Text = "Test prompt content"
                    }
                }
            }
        };

        _mockMcpService.GetPromptAsync(request).Returns(expectedResponse);

        // Act
        var result = await _controller.GetPrompt(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(McpGetPromptResponse));
        
        var response = (McpGetPromptResponse)okResult.Value!;
        Assert.AreEqual("Search prompt", response.Description);
        Assert.AreEqual(1, response.Messages?.Count);

        await _mockMcpService.Received(1).GetPromptAsync(request);
    }

    [TestMethod]
    public async Task ProcessNLWebQuery_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List
        };

        var expectedResponse = new NLWebResponse
        {
            QueryId = "mcp-test",
            Results = new List<NLWebResult>()
        };

        _mockMcpService.ProcessNLWebQueryAsync(request).Returns(expectedResponse);

        // Act
        var result = await _controller.ProcessNLWebQuery(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(NLWebResponse));
        
        var response = (NLWebResponse)okResult.Value!;
        Assert.AreEqual("mcp-test", response.QueryId);

        await _mockMcpService.Received(1).ProcessNLWebQueryAsync(request);
    }

    [TestMethod]
    public async Task ProcessNLWebQuery_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ProcessNLWebQuery(null!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.AreEqual("Invalid Request", problemDetails.Title);
        Assert.AreEqual(400, problemDetails.Status);
    }

    [TestMethod]
    public async Task CallTool_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new McpCallToolRequest
        {
            Name = "nlweb_search",
            Arguments = new Dictionary<string, object>()
        };        _mockMcpService
            .When(x => x.CallToolAsync(Arg.Any<McpCallToolRequest>()))
            .Throw(new Exception("Service error"));

        // Act
        var result = await _controller.CallTool(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = (ObjectResult)result;
        Assert.AreEqual(500, objectResult.StatusCode);
        Assert.IsInstanceOfType(objectResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)objectResult.Value!;
        Assert.AreEqual("Internal Server Error", problemDetails.Title);
        Assert.AreEqual(500, problemDetails.Status);
    }
}
