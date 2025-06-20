using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NLWebNet.Controllers;

/// <summary>
/// Controller for the NLWeb /ask endpoint.
/// Implements the core NLWeb protocol for natural language queries.
/// </summary>
[ApiController]
[Route("ask")]
[Produces("application/json")]
public class AskController : ControllerBase
{
    private readonly INLWebService _nlWebService;
    private readonly ILogger<AskController> _logger;

    public AskController(INLWebService nlWebService, ILogger<AskController> logger)
    {
        _nlWebService = nlWebService ?? throw new ArgumentNullException(nameof(nlWebService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Process a natural language query using the NLWeb protocol.
    /// Supports all three query modes: list, summarize, and generate.
    /// </summary>
    /// <param name="request">The NLWeb request containing the query and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NLWeb response with results</returns>
    [HttpPost]
    [ProducesResponseType<NLWebResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessQuery(
        [FromBody] NLWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the request
            if (request == null)
            {
                _logger.LogWarning("Received null request");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                _logger.LogWarning("Received request with empty query");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Query",
                    Detail = "Query parameter is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Generate query ID if not provided
            if (string.IsNullOrEmpty(request.QueryId))
            {
                request.QueryId = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated query ID: {QueryId}", request.QueryId);
            }

            _logger.LogInformation("Processing NLWeb query: {QueryId}, Mode: {Mode}, Query: {Query}", 
                request.QueryId, request.Mode, request.Query);

            // Check if streaming is requested
            if (request.Streaming == true)
            {
                return await ProcessStreamingQuery(request, cancellationToken);
            }

            // Process non-streaming query
            var response = await _nlWebService.ProcessRequestAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully processed query {QueryId} with {ResultCount} results", 
                response.QueryId, response.Results?.Count ?? 0);

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error for query {QueryId}: {Message}", 
                request?.QueryId, ex.Message);
            
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Query {QueryId} was cancelled", request?.QueryId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query {QueryId}: {Message}", 
                request?.QueryId, ex.Message);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Process a natural language query via GET request with query parameters.
    /// This provides a simple interface for basic queries.
    /// </summary>
    /// <param name="query">The natural language query</param>
    /// <param name="mode">Query mode (list, summarize, generate)</param>
    /// <param name="site">Site filter (optional)</param>
    /// <param name="streaming">Enable streaming responses (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NLWeb response with results</returns>
    [HttpGet]
    [ProducesResponseType<NLWebResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessQueryGet(
        [FromQuery, Required] string query,
        [FromQuery] QueryMode mode = QueryMode.List,
        [FromQuery] string? site = null,
        [FromQuery] bool streaming = true,
        CancellationToken cancellationToken = default)
    {
        var request = new NLWebRequest
        {
            Query = query,
            Mode = mode,
            Site = site,
            Streaming = streaming,
            QueryId = Guid.NewGuid().ToString()
        };

        return await ProcessQuery(request, cancellationToken);
    }

    /// <summary>
    /// Process a streaming query using Server-Sent Events.
    /// </summary>
    private async Task<IActionResult> ProcessStreamingQuery(
        NLWebRequest request, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting streaming response for query {QueryId}", request.QueryId);

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        try
        {
            await foreach (var response in _nlWebService.ProcessRequestStreamAsync(request, cancellationToken))
            {
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                _logger.LogDebug("Sent streaming chunk for query {QueryId}", request.QueryId);
            }

            // Send end-of-stream marker
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation("Completed streaming response for query {QueryId}", request.QueryId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming query {QueryId} was cancelled", request.QueryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming for query {QueryId}: {Message}", 
                request.QueryId, ex.Message);

            // Send error as SSE
            var errorResponse = new { error = "An error occurred during streaming" };
            var errorJson = JsonSerializer.Serialize(errorResponse);
            await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
        }

        return new EmptyResult();
    }
}
