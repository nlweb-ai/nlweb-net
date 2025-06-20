using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NLWebNet.Controllers;
using NLWebNet.Models;
using NLWebNet.Services;
using Microsoft.AspNetCore.Mvc;

namespace NLWebNet.Tests.Controllers;

[TestClass]
public class AskControllerTests
{
    private INLWebService _mockNLWebService = null!;
    private ILogger<AskController> _mockLogger = null!;
    private AskController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockNLWebService = Substitute.For<INLWebService>();
        _mockLogger = Substitute.For<ILogger<AskController>>();
        _controller = new AskController(_mockNLWebService, _mockLogger);
    }

    [TestMethod]
    public async Task ProcessQuery_ValidRequest_ReturnsOkResult()
    {        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List,
            QueryId = "test-123",
            Streaming = false  // Disable streaming for this test
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
        };        _mockNLWebService
            .ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);        // Act
        var result = await _controller.ProcessQuery(request);
            
        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.IsInstanceOfType(okResult.Value, typeof(NLWebResponse));
        
        var response = (NLWebResponse)okResult.Value!;
        Assert.AreEqual("test-123", response.QueryId);
        Assert.AreEqual(1, response.Results?.Count);

        await _mockNLWebService.Received(1).ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessQuery_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ProcessQuery(null!);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.AreEqual("Invalid Request", problemDetails.Title);
        Assert.AreEqual(400, problemDetails.Status);
    }

    [TestMethod]
    public async Task ProcessQuery_EmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "",
            Mode = QueryMode.List
        };

        // Act
        var result = await _controller.ProcessQuery(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ProblemDetails));
        
        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.AreEqual("Invalid Query", problemDetails.Title);
        Assert.AreEqual(400, problemDetails.Status);
    }

    [TestMethod]
    public async Task ProcessQuery_GeneratesQueryIdWhenMissing()
    {        // Arrange
        // QueryId is intentionally left null to test auto-generation
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List,
            Streaming = false
        };

        var expectedResponse = new NLWebResponse
        {
            QueryId = "generated-id",
            Results = new List<NLWebResult>()
        };

        _mockNLWebService
            .ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.ProcessQuery(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        
        // Verify that QueryId was generated (not null or empty)
        Assert.IsNotNull(request.QueryId);
        Assert.IsFalse(string.IsNullOrEmpty(request.QueryId));

        await _mockNLWebService.Received(1).ProcessRequestAsync(
            Arg.Is<NLWebRequest>(r => !string.IsNullOrEmpty(r.QueryId)), 
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessQueryGet_ValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new NLWebResponse
        {
            QueryId = "get-test",
            Results = new List<NLWebResult>()
        };

        _mockNLWebService
            .ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.ProcessQueryGet("test query", QueryMode.Summarize, "test-site", false);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        await _mockNLWebService.Received(1).ProcessRequestAsync(
            Arg.Is<NLWebRequest>(r => 
                r.Query == "test query" && 
                r.Mode == QueryMode.Summarize &&
                r.Site == "test-site" &&
                r.Streaming == false),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessQuery_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test query",
            Mode = QueryMode.List
        };        _mockNLWebService
            .When(x => x.ProcessRequestAsync(Arg.Any<NLWebRequest>(), Arg.Any<CancellationToken>()))
            .Throw(new Exception("Service error"));

        // Act
        var result = await _controller.ProcessQuery(request);

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
