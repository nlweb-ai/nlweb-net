using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLWebNet.Extensions;
using NLWebNet.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NLWebNet.Tests;

/// <summary>
/// Tests specifically for the Minimal API implementation to ensure endpoint registration
/// and parameter binding work correctly.
/// </summary>
[TestClass]
public class MinimalApiTests
{
    private WebApplicationFactory<TestStartup> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<TestStartup>();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task MinimalApi_AskEndpoint_ShouldBeRegistered()
    {
        // Act
        var response = await _client.GetAsync("/ask?query=test");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [TestMethod]
    public async Task MinimalApi_McpEndpoint_ShouldBeRegistered()
    {
        // Act
        var response = await _client.GetAsync("/mcp?query=test");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [TestMethod]
    public async Task MinimalApi_AskEndpoint_GET_ShouldBindQueryParameters()
    {
        // Act
        var response = await _client.GetAsync("/ask?query=test+search&mode=List&site=example&streaming=false");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("test search"), "Response should contain the query");
    }

    [TestMethod]
    public async Task MinimalApi_AskEndpoint_POST_ShouldBindJsonBody()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test search",
            Mode = QueryMode.Summarize,
            Site = "example",
            Streaming = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/ask", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("test search"), "Response should contain the query");
    }

    [TestMethod]
    public async Task MinimalApi_McpEndpoint_POST_ShouldBindJsonBody()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "test search",
            Mode = QueryMode.Generate,
            Streaming = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseJson = await response.Content.ReadAsStringAsync();
        var nlwebResponse = JsonSerializer.Deserialize<NLWebResponse>(responseJson);

        Assert.IsNotNull(nlwebResponse);
        Assert.IsNotNull(nlwebResponse.QueryId);
        Assert.IsNotNull(nlwebResponse.Results);
    }

    [TestMethod]
    public async Task MinimalApi_StreamingEndpoint_ShouldReturnServerSentEvents()
    {
        // Act
        var response = await _client.GetAsync("/ask?query=test&streaming=true");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        // Streaming responses should contain incremental content
        Assert.IsTrue(content.Length > 0, "Streaming response should have content");
    }

    [TestMethod]
    public async Task MinimalApi_InvalidRequest_ShouldReturnBadRequest()
    {
        // Act - Missing required query parameter
        var response = await _client.GetAsync("/ask");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task MinimalApi_OpenApiSchema_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync(); Assert.IsTrue(content.Contains("\"openapi\":"), "Should contain OpenAPI specification");
        Assert.IsTrue(content.Contains("\"/ask\""), "Should document the /ask endpoint");
        Assert.IsTrue(content.Contains("\"/mcp\""), "Should document the /mcp endpoint");
    }
}

/// <summary>
/// Test startup class for WebApplicationFactory integration tests
/// </summary>
public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNLWebNet();
        services.AddOpenApi();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseNLWebNet();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapNLWebNet();
            endpoints.MapOpenApi();
        });
    }
}
