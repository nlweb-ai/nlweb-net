using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NLWebNet.Endpoints;

/// <summary>
/// Minimal API endpoints for the NLWeb /ask functionality.
/// Implements the core NLWeb protocol for natural language queries.
/// </summary>
public static class AskEndpoints
{
    /// <summary>
    /// Maps the /ask endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The route group builder for further configuration</returns>
    public static RouteGroupBuilder MapAskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ask")
            .WithTags("Ask")
            .WithOpenApi();

        // POST /ask - Main endpoint for processing queries
        group.MapPost("/", ProcessQueryAsync)
            .WithName("ProcessQuery")
            .WithSummary("Process a natural language query using the NLWeb protocol")
            .WithDescription("Supports all three query modes: list, summarize, and generate.")
            .Produces<NLWebResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /ask/stream - Streaming endpoint for real-time responses
        group.MapGet("/stream", ProcessStreamingQueryAsync)
            .WithName("ProcessStreamingQuery")
            .WithSummary("Process a streaming natural language query")
            .WithDescription("Provides real-time streaming responses for better user experience.")
            .Produces<string>(StatusCodes.Status200OK, "text/plain")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /ask - Simple query endpoint for basic queries
        group.MapGet("/", ProcessSimpleQueryAsync)
            .WithName("ProcessSimpleQuery")
            .WithSummary("Process a simple query via GET request")
            .WithDescription("Simplified endpoint for basic queries using query parameters.")
            .Produces<NLWebResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }    /// <summary>
         /// Process a natural language query using the NLWeb protocol.
         /// Supports all three query modes: list, summarize, and generate.
         /// </summary>
         /// <param name="request">The NLWeb request containing the query and options</param>
         /// <param name="nlWebService">The NLWeb service</param>
         /// <param name="loggerFactory">The logger factory</param>
         /// <param name="cancellationToken">Cancellation token</param>
         /// <returns>NLWeb response with results</returns>
    private static async Task<Results<Ok<NLWebResponse>, BadRequest<ProblemDetails>, StatusCodeHttpResult>> ProcessQueryAsync(
        [FromBody] NLWebRequest request,
        INLWebService nlWebService, ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger(nameof(AskEndpoints));

        logger.LogInformation("[ENTRY] /ask POST ProcessQueryAsync. Request: {@Request}", request);
        if (request != null)
        {
            logger.LogDebug("Request.Query: {Query}, Request.Mode: {Mode}, Request.Site: {Site}, Request.QueryId: {QueryId}",
                request.Query, request.Mode, request.Site, request.QueryId);
        }

        try
        {
            if (request == null)
            {
                logger.LogWarning("Received null request");
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request body is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                logger.LogWarning("Received request with empty query");
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Query parameter is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }
            logger.LogInformation("[PROCESSING] NLWeb query: {Query} (Mode: {Mode}, QueryId: {QueryId})",
                request.Query, request.Mode, request.QueryId);

            logger.LogDebug("Calling ProcessRequestAsync in NLWebService for QueryId={QueryId}", request.QueryId);
            var response = await nlWebService.ProcessRequestAsync(request, cancellationToken);
            logger.LogDebug("ProcessRequestAsync in NLWebService complete for QueryId={QueryId}", response.QueryId);

            logger.LogInformation("[SUCCESS] Processed query {QueryId} with {ResultCount} results",
                response.QueryId, response.Results?.Count ?? 0);

            logger.LogInformation("[EXIT] /ask POST ProcessQueryAsync for QueryId={QueryId}", response.QueryId);
            return TypedResults.Ok(response);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error processing query: {Message}", ex.Message);
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FAIL] Error processing NLWeb query: {Message}", ex.Message);
            return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    /// <summary>
         /// Process a streaming natural language query with server-sent events.
         /// </summary>
         /// <param name="query">The natural language query</param>
         /// <param name="nlWebService">The NLWeb service</param>
         /// <param name="loggerFactory">The logger factory</param>
         /// <param name="mode">Query processing mode (list, summarize, generate)</param>
         /// <param name="site">Optional site parameter for scoped searches</param>
         /// <param name="prev">Comma-separated list of previous queries</param>         /// <param name="decontextualizedQuery">Pre-decontextualized query</param>
         /// <param name="queryId">Optional query ID for correlation</param>
         /// <param name="cancellationToken">Cancellation token</param>
         /// <returns>Streaming response</returns>
    private static Task<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>> ProcessStreamingQueryAsync(
        [FromQuery] string query,
        INLWebService nlWebService,
        ILoggerFactory loggerFactory,
        [FromQuery] string? mode = null,
        [FromQuery] string? site = null,
        [FromQuery] string? prev = null,
        [FromQuery] string? decontextualizedQuery = null,
        [FromQuery] string? queryId = null, CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger(nameof(AskEndpoints));

        logger.LogInformation("[ENTRY] /ask/stream GET ProcessStreamingQueryAsync. Query: {Query}, Mode: {Mode}, QueryId: {QueryId}", query, mode, queryId);
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                logger.LogWarning("Received streaming request with empty query");
                return Task.FromResult<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>>(TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Query parameter is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                }));
            }

            var queryMode = QueryMode.List;
            if (!string.IsNullOrWhiteSpace(mode))
            {
                if (!Enum.TryParse<QueryMode>(mode, true, out queryMode))
                {
                    return Task.FromResult<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>>(TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = $"Invalid mode '{mode}'. Valid values are: {string.Join(", ", Enum.GetNames<QueryMode>())}",
                        Status = StatusCodes.Status400BadRequest
                    }));
                }
            }

            var request = new NLWebRequest
            {
                Query = query,
                Mode = queryMode,
                Site = site,
                Prev = prev,
                DecontextualizedQuery = decontextualizedQuery,
                QueryId = queryId,
                Streaming = true
            };

            logger.LogInformation("[PROCESSING] Streaming NLWeb query: {Query} (Mode: {Mode}, QueryId: {QueryId})",
                request.Query, request.Mode, request.QueryId);

            logger.LogDebug("Calling ProcessRequestStreamAsync in NLWebService for QueryId={QueryId}", request.QueryId);
            var streamingResults = nlWebService.ProcessRequestStreamAsync(request, cancellationToken);
            logger.LogDebug("ProcessRequestStreamAsync in NLWebService started for QueryId={QueryId}", request.QueryId);

            return Task.FromResult<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>>(TypedResults.Stream(async stream =>
            {
                var writer = new StreamWriter(stream);
                await writer.WriteLineAsync("Content-Type: text/event-stream");
                await writer.WriteLineAsync("Cache-Control: no-cache");
                await writer.WriteLineAsync("Connection: keep-alive");
                await writer.WriteLineAsync();
                await writer.FlushAsync();

                try
                {
                    await foreach (var chunk in streamingResults.WithCancellation(cancellationToken))
                    {
                        logger.LogTrace("[STREAM] Sending chunk for QueryId={QueryId}", request.QueryId);
                        await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(chunk)}");
                        await writer.WriteLineAsync();
                        await writer.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[STREAM] Exception while streaming for QueryId={QueryId}", request.QueryId);
                }

                // Send end-of-stream marker
                await writer.WriteLineAsync("data: [DONE]");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }, "text/event-stream"));
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error processing streaming query: {Message}", ex.Message);
            return Task.FromResult<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>>(TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FAIL] Error processing streaming NLWeb query: {Message}", ex.Message);
            return Task.FromResult<Results<BadRequest<ProblemDetails>, PushStreamHttpResult>>(TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing the streaming request",
                Status = StatusCodes.Status500InternalServerError
            }));
        }
    }    /// <summary>
         /// Process a simple query via GET request with query parameters.
         /// </summary>
         /// <param name="query">The natural language query</param>
         /// <param name="nlWebService">The NLWeb service</param>
         /// <param name="loggerFactory">The logger factory</param>
         /// <param name="mode">Query processing mode (list, summarize, generate)</param>
         /// <param name="site">Optional site parameter for scoped searches</param>
         /// <param name="prev">Comma-separated list of previous queries</param>
         /// <param name="decontextualizedQuery">Pre-decontextualized query</param>
         /// <param name="queryId">Optional query ID for correlation</param>
         /// <param name="cancellationToken">Cancellation token</param>
         /// <returns>NLWeb response with results</returns>
    private static async Task<Results<Ok<NLWebResponse>, BadRequest<ProblemDetails>, StatusCodeHttpResult>> ProcessSimpleQueryAsync(
        [FromQuery] string query,
        INLWebService nlWebService,
        ILoggerFactory loggerFactory,
        [FromQuery] string? mode = null,
        [FromQuery] string? site = null, [FromQuery] string? prev = null,
        [FromQuery] string? decontextualizedQuery = null,
        [FromQuery] string? queryId = null,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger(nameof(AskEndpoints));

        logger.LogInformation("[ENTRY] /ask GET ProcessSimpleQueryAsync. Query: {Query}, Mode: {Mode}, QueryId: {QueryId}", query, mode, queryId);
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                logger.LogWarning("Received simple request with empty query");
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Query parameter is required and cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var queryMode = QueryMode.List;
            if (!string.IsNullOrWhiteSpace(mode))
            {
                if (!Enum.TryParse<QueryMode>(mode, true, out queryMode))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = $"Invalid mode '{mode}'. Valid values are: {string.Join(", ", Enum.GetNames<QueryMode>())}",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            }

            var request = new NLWebRequest
            {
                Query = query,
                Mode = queryMode,
                Site = site,
                Prev = prev,
                DecontextualizedQuery = decontextualizedQuery,
                QueryId = queryId,
                Streaming = false
            };

            logger.LogInformation("[PROCESSING] Simple NLWeb query: {Query} (Mode: {Mode}, QueryId: {QueryId})",
                request.Query, request.Mode, request.QueryId);

            logger.LogDebug("Calling ProcessRequestAsync in NLWebService for QueryId={QueryId}", request.QueryId);
            var response = await nlWebService.ProcessRequestAsync(request, cancellationToken);
            logger.LogDebug("ProcessRequestAsync in NLWebService complete for QueryId={QueryId}", response.QueryId);

            logger.LogInformation("[SUCCESS] Processed simple query {QueryId} with {ResultCount} results",
                response.QueryId, response.Results?.Count ?? 0);

            logger.LogInformation("[EXIT] /ask GET ProcessSimpleQueryAsync for QueryId={QueryId}", response.QueryId);
            return TypedResults.Ok(response);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error processing simple query: {Message}", ex.Message);
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FAIL] Error processing simple NLWeb query: {Message}", ex.Message);
            return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
