using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NLWebNet.MCP;
using NLWebNet.Models;
using System.Text.Json;

namespace NLWebNet.Endpoints;

/// <summary>
/// Minimal API endpoints for the NLWeb /mcp functionality.
/// Implements the Model Context Protocol for AI client integration.
/// </summary>
public static class McpEndpoints
{
    /// <summary>
    /// Maps the /mcp endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The route group builder for further configuration</returns>
    public static RouteGroupBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/mcp")
            .WithTags("MCP")
            .WithOpenApi();

        // POST /mcp/list_tools - List available MCP tools
        group.MapPost("/list_tools", ListToolsAsync)
            .WithName("ListTools")
            .WithSummary("List available MCP tools")
            .WithDescription("Returns a list of available tools with their schemas")
            .Produces<McpListToolsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /mcp/tools - Alternative endpoint for listing tools
        group.MapGet("/tools", ListToolsAsync)
            .WithName("ListToolsGet")
            .WithSummary("List available MCP tools (GET)")
            .WithDescription("Returns a list of available tools with their schemas")
            .Produces<McpListToolsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // POST /mcp/list_prompts - List available MCP prompts
        group.MapPost("/list_prompts", ListPromptsAsync)
            .WithName("ListPrompts")
            .WithSummary("List available MCP prompts")
            .WithDescription("Returns a list of available prompts with their schemas")
            .Produces<McpListPromptsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /mcp/prompts - Alternative endpoint for listing prompts
        group.MapGet("/prompts", ListPromptsAsync)
            .WithName("ListPromptsGet")
            .WithSummary("List available MCP prompts (GET)")
            .WithDescription("Returns a list of available prompts with their schemas")
            .Produces<McpListPromptsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // POST /mcp/call_tool - Call an MCP tool
        group.MapPost("/call_tool", CallToolAsync)
            .WithName("CallTool")
            .WithSummary("Call an MCP tool with the specified arguments")
            .WithDescription("Executes a tool with the provided arguments and returns the result")
            .Produces<McpCallToolResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // POST /mcp/get_prompt - Get a specific prompt with arguments
        group.MapPost("/get_prompt", GetPromptAsync)
            .WithName("GetPrompt")
            .WithSummary("Get a specific prompt with template substitution")
            .WithDescription("Returns a prompt with arguments substituted into the template")
            .Produces<McpGetPromptResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // POST /mcp - Unified MCP endpoint for compatibility
        group.MapPost("/", ProcessMcpRequestAsync)
            .WithName("ProcessMcpRequest")
            .WithSummary("Process a unified MCP request")
            .WithDescription("Handles various MCP request types in a single endpoint")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError); return group;
    }    /// <summary>
    /// List available MCP tools.
    /// </summary>
    /// <param name="mcpService">The MCP service</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <returns>List of available tools with their schemas</returns>
    private static async Task<IResult> ListToolsAsync(
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory)
    {        try
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogDebug("Listing available MCP tools");

            var response = await mcpService.ListToolsAsync();

            logger.LogInformation("Listed {ToolCount} MCP tools", response.Tools?.Count ?? 0);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error listing MCP tools: {Message}", ex.Message);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    /// <summary>
         /// List available MCP prompts.
         /// </summary>
         /// <param name="mcpService">The MCP service</param>
         /// <param name="loggerFactory">The logger factory</param>
         /// <returns>List of available prompts with their schemas</returns>
    private static async Task<IResult> ListPromptsAsync(
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory)
    {        try
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogDebug("Listing available MCP prompts");

            var response = await mcpService.ListPromptsAsync();

            logger.LogInformation("Listed {PromptCount} MCP prompts", response.Prompts?.Count ?? 0);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error listing MCP prompts: {Message}", ex.Message);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    /// <summary>
    /// Call an MCP tool with the specified arguments.
    /// </summary>
    /// <param name="request">Tool call request with name and arguments</param>
    /// <param name="mcpService">The MCP service</param>    
    /// <param name="loggerFactory">The logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    private static async Task<IResult> CallToolAsync(
        [FromBody] McpCallToolRequest request,
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {        try
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            
            // Validate the request
            if (request == null)
            {
                logger.LogWarning("Received null tool call request");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                logger.LogWarning("Received tool call request with empty name");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Tool name is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Calling MCP tool: {ToolName} with {ArgCount} arguments",
                request.Name, request.Arguments?.Count ?? 0);

            var response = await mcpService.CallToolAsync(request, cancellationToken);

            if (response.IsError == true && !string.IsNullOrEmpty(response.Content?.FirstOrDefault()?.Text))
            {
                var errorMessage = response.Content.FirstOrDefault()?.Text;
                if (errorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    logger.LogWarning("Tool not found: {ToolName}", request.Name);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Tool Not Found",
                        Detail = errorMessage,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                logger.LogWarning("Tool call error: {ErrorMessage}", errorMessage);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Tool Call Error",
                    Detail = errorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Successfully called tool {ToolName}", request.Name);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error calling MCP tool {ToolName}: {Message}", request?.Name, ex.Message);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    /// <summary>
    /// Get a specific prompt with template substitution.
    /// </summary>    
    /// <param name="request">Prompt request with name and arguments</param>
    /// <param name="mcpService">The MCP service</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prompt with substituted arguments</returns>
    private static async Task<IResult> GetPromptAsync(
        [FromBody] McpGetPromptRequest request,
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {        try
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            
            // Validate the request
            if (request == null)
            {
                logger.LogWarning("Received null prompt request");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                logger.LogWarning("Received prompt request with empty name");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Prompt name is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Getting MCP prompt: {PromptName} with {ArgCount} arguments",
                request.Name, request.Arguments?.Count ?? 0);

            var response = await mcpService.GetPromptAsync(request, cancellationToken);

            if (response.Messages == null || !response.Messages.Any())
            {
                logger.LogWarning("Prompt not found: {PromptName}", request.Name);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Prompt Not Found",
                    Detail = $"Prompt '{request.Name}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            logger.LogInformation("Successfully retrieved prompt {PromptName}", request.Name);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error getting MCP prompt {PromptName}: {Message}", request?.Name, ex.Message);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    /// <summary>
    /// Process a unified MCP request (for compatibility with existing clients).
    /// </summary>    
    /// <param name="request">Unified MCP request</param>
    /// <param name="mcpService">The MCP service</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MCP response based on request type</returns>
    private static async Task<IResult> ProcessMcpRequestAsync(
        [FromBody] object request,
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {        try
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            
            if (request == null)
            {
                logger.LogWarning("Received null MCP request");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Try to determine the request type based on the JSON structure
            var requestJson = JsonSerializer.Serialize(request);
            using var document = JsonDocument.Parse(requestJson);
            var root = document.RootElement;

            logger.LogDebug("Processing unified MCP request: {RequestType}", root.ToString());

            // Handle based on request structure
            if (root.TryGetProperty("method", out var methodElement))
            {
                var method = methodElement.GetString();
                logger.LogInformation("Processing MCP method: {Method}", method);

                return method?.ToLowerInvariant() switch
                {
                    "list_tools" => await ListToolsAsync(mcpService, loggerFactory),
                    "list_prompts" => await ListPromptsAsync(mcpService, loggerFactory),
                    "call_tool" => await HandleCallToolFromUnified(root, mcpService, loggerFactory, cancellationToken),
                    "get_prompt" => await HandleGetPromptFromUnified(root, mcpService, loggerFactory, cancellationToken),
                    _ => Results.BadRequest(new ProblemDetails
                    {
                        Title = "Unknown Method",
                        Detail = $"Unknown MCP method: {method}",
                        Status = StatusCodes.Status400BadRequest
                    })
                };
            }

            // Fallback: try to infer from properties
            if (root.TryGetProperty("name", out _))
            {
                if (root.TryGetProperty("arguments", out _))
                {
                    // Looks like a tool call or prompt request
                    var toolCallRequest = JsonSerializer.Deserialize<McpCallToolRequest>(requestJson);
                    if (toolCallRequest != null)
                    {
                        return await CallToolAsync(toolCallRequest, mcpService, loggerFactory, cancellationToken);
                    }
                }
            }

            logger.LogWarning("Could not determine MCP request type");
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Could not determine MCP request type",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error processing unified MCP request: {Message}", ex.Message);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    private static async Task<IResult> HandleCallToolFromUnified(
        JsonElement root,
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {        try
        {
            var request = JsonSerializer.Deserialize<McpCallToolRequest>(root.GetRawText());
            if (request != null)
            {
                return await CallToolAsync(request, mcpService, loggerFactory, cancellationToken);
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Tool Call",
                Detail = "Could not parse tool call request",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error handling unified tool call: {Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    private static async Task<IResult> HandleGetPromptFromUnified(
        JsonElement root,
        [FromServices] IMcpService mcpService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {        try
        {
            var request = JsonSerializer.Deserialize<McpGetPromptRequest>(root.GetRawText());
            if (request != null)
            {
                return await GetPromptAsync(request, mcpService, loggerFactory, cancellationToken);
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Prompt Request",
                Detail = "Could not parse prompt request",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(typeof(McpEndpoints));
            logger.LogError(ex, "Error handling unified prompt request: {Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
