using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLWebNet.MCP;
using NLWebNet.Models;
using System.Text.Json;

namespace NLWebNet.Controllers;

/// <summary>
/// Controller for the NLWeb /mcp endpoint.
/// Implements the Model Context Protocol for AI client integration.
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<McpController> _logger;

    public McpController(IMcpService mcpService, ILogger<McpController> logger)
    {
        _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// List available MCP tools.
    /// </summary>
    /// <returns>List of available tools with their schemas</returns>
    [HttpPost("list_tools")]
    [HttpGet("tools")]
    [ProducesResponseType<McpListToolsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListTools()
    {
        try
        {
            _logger.LogDebug("Listing available MCP tools");

            var response = await _mcpService.ListToolsAsync();

            _logger.LogInformation("Listed {ToolCount} MCP tools", response.Tools?.Count ?? 0);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing MCP tools: {Message}", ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while listing tools",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// List available MCP prompts.
    /// </summary>
    /// <returns>List of available prompts with their schemas</returns>
    [HttpPost("list_prompts")]
    [HttpGet("prompts")]
    [ProducesResponseType<McpListPromptsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListPrompts()
    {
        try
        {
            _logger.LogDebug("Listing available MCP prompts");

            var response = await _mcpService.ListPromptsAsync();

            _logger.LogInformation("Listed {PromptCount} MCP prompts", response.Prompts?.Count ?? 0);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing MCP prompts: {Message}", ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while listing prompts",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Call an MCP tool with the specified arguments.
    /// </summary>
    /// <param name="request">Tool call request with name and arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    [HttpPost("call_tool")]
    [ProducesResponseType<McpCallToolResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CallTool(
        [FromBody] McpCallToolRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Received null MCP tool call request");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Received MCP tool call request with empty tool name");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Tool Name",
                    Detail = "Tool name is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Calling MCP tool: {ToolName} with {ArgCount} arguments",
                request.Name, request.Arguments?.Count ?? 0);

            var response = await _mcpService.CallToolAsync(request);

            if (response.IsError)
            {
                _logger.LogWarning("MCP tool call failed: {ToolName}, Error: {Error}",
                    request.Name, response.Content?.FirstOrDefault()?.Text);
            }
            else
            {
                _logger.LogInformation("Successfully called MCP tool: {ToolName}", request.Name);
            }

            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument in MCP tool call: {Message}", ex.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Arguments",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MCP tool call was cancelled: {ToolName}", request?.Name);
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool {ToolName}: {Message}",
                request?.Name, ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while calling the tool",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get an MCP prompt with argument substitution.
    /// </summary>
    /// <param name="request">Prompt request with name and arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prompt with substituted arguments</returns>
    [HttpPost("get_prompt")]
    [ProducesResponseType<McpGetPromptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPrompt(
        [FromBody] McpGetPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Received null MCP prompt request");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Received MCP prompt request with empty prompt name");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Prompt Name",
                    Detail = "Prompt name is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Getting MCP prompt: {PromptName} with {ArgCount} arguments",
                request.Name, request.Arguments?.Count ?? 0);

            var response = await _mcpService.GetPromptAsync(request);

            if (response.Messages?.Any() == true &&
                response.Messages.Any(m => m.Content?.Text?.Contains("Error:") == true))
            {
                _logger.LogWarning("MCP prompt request failed: {PromptName}, Error: {Error}",
                    request.Name, response.Description);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved MCP prompt: {PromptName}", request.Name);
            }

            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument in MCP prompt request: {Message}", ex.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Arguments",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MCP prompt request was cancelled: {PromptName}", request?.Name);
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP prompt {PromptName}: {Message}",
                request?.Name, ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while getting the prompt",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Process an NLWeb query through the MCP interface.
    /// This provides direct access to NLWeb functionality for MCP clients.
    /// </summary>
    /// <param name="request">NLWeb request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NLWeb response formatted for MCP consumption</returns>
    [HttpPost("query")]
    [ProducesResponseType<NLWebResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessNLWebQuery(
        [FromBody] NLWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Received null NLWeb request via MCP");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                _logger.LogWarning("Received NLWeb request with empty query via MCP");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Query",
                    Detail = "Query parameter is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Processing NLWeb query via MCP: {Query}, Mode: {Mode}",
                request.Query, request.Mode);

            var response = await _mcpService.ProcessNLWebQueryAsync(request);

            _logger.LogInformation("Successfully processed NLWeb query via MCP: {QueryId}",
                response.QueryId);

            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument in NLWeb query via MCP: {Message}", ex.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Arguments",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("NLWeb query via MCP was cancelled: {Query}", request?.Query);
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NLWeb query via MCP: {Message}", ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing the query",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
