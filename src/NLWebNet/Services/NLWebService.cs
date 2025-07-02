using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Runtime.CompilerServices;

namespace NLWebNet.Services;

/// <summary>
/// Main implementation of the NLWeb service that orchestrates query processing and result generation.
/// </summary>
public class NLWebService : INLWebService
{
    private readonly IQueryProcessor _queryProcessor;
    private readonly IResultGenerator _resultGenerator;
    private readonly IDataBackend? _dataBackend;
    private readonly IBackendManager? _backendManager;
    private readonly IToolSelector? _toolSelector;
    private readonly IToolExecutor? _toolExecutor;
    private readonly ILogger<NLWebService> _logger;
    private readonly NLWebOptions _options;

    /// <summary>
    /// Constructor for single-backend mode (backward compatibility).
    /// </summary>
    public NLWebService(
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator,
        IDataBackend dataBackend,
        ILogger<NLWebService> logger,
        IOptions<NLWebOptions> options)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        _resultGenerator = resultGenerator ?? throw new ArgumentNullException(nameof(resultGenerator));
        _dataBackend = dataBackend ?? throw new ArgumentNullException(nameof(dataBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Constructor for multi-backend mode.
    /// </summary>
    public NLWebService(
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator,
        IBackendManager backendManager,
        ILogger<NLWebService> logger,
        IOptions<NLWebOptions> options)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        _resultGenerator = resultGenerator ?? throw new ArgumentNullException(nameof(resultGenerator));
        _backendManager = backendManager ?? throw new ArgumentNullException(nameof(backendManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Constructor for single-backend mode with tool support.
    /// </summary>
    public NLWebService(
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator,
        IDataBackend dataBackend,
        IToolSelector toolSelector,
        IToolExecutor toolExecutor,
        ILogger<NLWebService> logger,
        IOptions<NLWebOptions> options)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        _resultGenerator = resultGenerator ?? throw new ArgumentNullException(nameof(resultGenerator));
        _dataBackend = dataBackend ?? throw new ArgumentNullException(nameof(dataBackend));
        _toolSelector = toolSelector ?? throw new ArgumentNullException(nameof(toolSelector));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Constructor for multi-backend mode with tool support.
    /// </summary>
    public NLWebService(
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator,
        IBackendManager backendManager,
        IToolSelector toolSelector,
        IToolExecutor toolExecutor,
        ILogger<NLWebService> logger,
        IOptions<NLWebOptions> options)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        _resultGenerator = resultGenerator ?? throw new ArgumentNullException(nameof(resultGenerator));
        _backendManager = backendManager ?? throw new ArgumentNullException(nameof(backendManager));
        _toolSelector = toolSelector ?? throw new ArgumentNullException(nameof(toolSelector));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<NLWebResponse> ProcessRequestAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[START] ProcessRequestAsync for QueryId={QueryId}, Query='{Query}'", request.QueryId, request.Query);

        try
        {
            _logger.LogDebug("Validating request for QueryId={QueryId}", request.QueryId);
            if (!_queryProcessor.ValidateRequest(request))
            {
                _logger.LogWarning("Request validation failed for QueryId={QueryId}", request.QueryId);
                return CreateErrorResponse("Invalid request. Please check your query and try again.", request.QueryId);
            }

            var queryId = _queryProcessor.GenerateQueryId(request);
            _logger.LogDebug("Generated QueryId={QueryId}", queryId);

            _logger.LogDebug("Calling ProcessQueryAsync for QueryId={QueryId}", queryId);
            var processedQuery = await _queryProcessor.ProcessQueryAsync(request, cancellationToken);
            _logger.LogDebug("ProcessQueryAsync complete for QueryId={QueryId}", queryId);

            // Check if tool execution is available and enabled
            if (_toolSelector != null && _toolExecutor != null && _options.ToolSelectionEnabled)
            {
                _logger.LogDebug("Tool execution enabled, checking if tool selection is needed for QueryId={QueryId}", queryId);

                if (_toolSelector.ShouldSelectTool(request))
                {
                    _logger.LogDebug("Tool selection needed for QueryId={QueryId}", queryId);

                    var selectedTool = await _toolSelector.SelectToolAsync(request, cancellationToken);
                    if (!string.IsNullOrEmpty(selectedTool))
                    {
                        _logger.LogInformation("Tool '{Tool}' selected for QueryId={QueryId}, executing tool", selectedTool, queryId);

                        try
                        {
                            var toolResponse = await _toolExecutor.ExecuteToolAsync(request, selectedTool, cancellationToken);
                            _logger.LogInformation("[END] Tool execution completed for QueryId={QueryId} with tool '{Tool}'", queryId, selectedTool);
                            return toolResponse;
                        }
                        catch (Exception toolEx)
                        {
                            _logger.LogError(toolEx, "Tool execution failed for QueryId={QueryId} with tool '{Tool}', falling back to standard processing", queryId, selectedTool);
                            // Fall through to standard processing
                        }
                    }
                }
            }

            _logger.LogDebug("Using standard processing pipeline for QueryId={QueryId}", queryId);
            _logger.LogDebug("Calling GenerateListAsync for QueryId={QueryId}", queryId);
            var searchResults = await _resultGenerator.GenerateListAsync(processedQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();
            _logger.LogDebug("GenerateListAsync complete for QueryId={QueryId}, ResultCount={ResultCount}", queryId, resultsList.Count);

            _logger.LogDebug("Calling GenerateResponseByModeAsync for QueryId={QueryId}", queryId);
            var response = await GenerateResponseByModeAsync(request.Mode, processedQuery, resultsList, queryId, cancellationToken);
            _logger.LogDebug("GenerateResponseByModeAsync complete for QueryId={QueryId}", queryId);

            _logger.LogInformation("[END] Successfully processed request {QueryId} with {ResultCount} results", queryId, resultsList.Count);
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request processing was cancelled for QueryId={QueryId}", request.QueryId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NLWeb request for QueryId={QueryId}", request.QueryId);
            return CreateErrorResponse("An error occurred while processing your request. Please try again.", request.QueryId);
        }
    }    /// <inheritdoc />
    public async IAsyncEnumerable<NLWebResponse> ProcessRequestStreamAsync(
        NLWebRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[START] ProcessRequestStreamAsync for QueryId={QueryId}, Query='{Query}'", request.QueryId, request.Query);

        if (!_queryProcessor.ValidateRequest(request))
        {
            _logger.LogWarning("Streaming request validation failed for QueryId={QueryId}", request.QueryId);
            yield return CreateErrorResponse("Invalid request. Please check your query and try again.", request.QueryId);
            yield break;
        }

        var queryId = _queryProcessor.GenerateQueryId(request);
        _logger.LogDebug("Generated QueryId={QueryId} (streaming)", queryId);

        List<NLWebResponse> bufferedResponses = new();
        bool hadException = false;
        try
        {
            _logger.LogDebug("Calling ProcessRequestStreamInternalAsync for QueryId={QueryId}", queryId);
            await foreach (var response in ProcessRequestStreamInternalAsync(request, queryId, cancellationToken))
            {
                bufferedResponses.Add(response);
            }
            _logger.LogInformation("[END] ProcessRequestStreamAsync completed for QueryId={QueryId}", queryId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming request processing was cancelled for QueryId={QueryId}", request.QueryId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessRequestStreamAsync for QueryId={QueryId}", request.QueryId);
            bufferedResponses.Clear();
            bufferedResponses.Add(CreateErrorResponse("An error occurred while processing your request. Please try again.", request.QueryId));
            hadException = true;
        }

        foreach (var response in bufferedResponses)
        {
            yield return response;
        }
        if (hadException)
            yield break;
    }

    /// <summary>
    /// Internal implementation of streaming request processing with proper exception handling.
    /// </summary>
    private async IAsyncEnumerable<NLWebResponse> ProcessRequestStreamInternalAsync(
        NLWebRequest request,
        string queryId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Check if tool execution is available and enabled first
        if (_toolSelector != null && _toolExecutor != null && _options.ToolSelectionEnabled)
        {
            _logger.LogDebug("[StreamInternal] Tool execution enabled, checking if tool selection is needed for QueryId={QueryId}", queryId);

            if (_toolSelector.ShouldSelectTool(request))
            {
                _logger.LogDebug("[StreamInternal] Tool selection needed for QueryId={QueryId}", queryId);

                var toolResponse = await TryExecuteToolAsync(request, queryId, cancellationToken);
                if (toolResponse != null)
                {
                    yield return toolResponse;
                    yield break;
                }
                // If tool execution failed, fall through to standard processing
            }
        }

        // Standard processing pipeline
        await foreach (var response in ProcessStandardStreamingAsync(request, queryId, cancellationToken))
        {
            yield return response;
        }
    }

    private async Task<NLWebResponse?> TryExecuteToolAsync(NLWebRequest request, string queryId, CancellationToken cancellationToken)
    {
        try
        {
            var selectedTool = await _toolSelector!.SelectToolAsync(request, cancellationToken);
            if (!string.IsNullOrEmpty(selectedTool))
            {
                _logger.LogInformation("[StreamInternal] Tool '{Tool}' selected for QueryId={QueryId}, executing tool", selectedTool, queryId);

                var toolResponse = await _toolExecutor!.ExecuteToolAsync(request, selectedTool, cancellationToken);
                _logger.LogInformation("[StreamInternal] Tool execution completed for QueryId={QueryId} with tool '{Tool}'", queryId, selectedTool);
                return toolResponse;
            }
        }
        catch (Exception toolEx)
        {
            _logger.LogError(toolEx, "[StreamInternal] Tool execution failed for QueryId={QueryId}, falling back to standard processing", queryId);
        }
        return null;
    }

    private async IAsyncEnumerable<NLWebResponse> ProcessStandardStreamingAsync(
        NLWebRequest request,
        string queryId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<NLWebResponse>? responseStream = null;
        Exception? processingException = null;

        List<NLWebResponse> bufferedResponses = new();
        bool hadException = false;
        try
        {
            _logger.LogDebug("[StreamInternal] Using standard processing pipeline for QueryId={QueryId}", queryId);
            _logger.LogDebug("[StreamInternal] Calling ProcessQueryAsync for QueryId={QueryId}", queryId);
            var processedQuery = await _queryProcessor.ProcessQueryAsync(request, cancellationToken);
            _logger.LogDebug("[StreamInternal] ProcessQueryAsync complete for QueryId={QueryId}", queryId);

            _logger.LogDebug("[StreamInternal] Calling GenerateListAsync for QueryId={QueryId}", queryId);
            var searchResults = await _resultGenerator.GenerateListAsync(processedQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();
            _logger.LogDebug("[StreamInternal] GenerateListAsync complete for QueryId={QueryId}, ResultCount={ResultCount}", queryId, resultsList.Count);

            responseStream = GenerateStreamingResponsesAsync(request, queryId, processedQuery, resultsList, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[StreamInternal] Request processing was cancelled for QueryId={QueryId}", queryId);
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
            hadException = true;
        }

        if (responseStream != null)
        {
            await foreach (var response in responseStream)
            {
                bufferedResponses.Add(response);
            }
        }
        else if (hadException)
        {
            _logger.LogError(processingException, "[StreamInternal] Error processing streaming NLWeb request for QueryId={QueryId}", queryId);
            bufferedResponses.Add(CreateErrorResponse("An error occurred while processing your request. Please try again.", queryId));
        }

        foreach (var response in bufferedResponses)
        {
            yield return response;
        }
    }

    /// <summary>
    /// Generates the streaming responses for a successful request.
    /// </summary>
    private async IAsyncEnumerable<NLWebResponse> GenerateStreamingResponsesAsync(
        NLWebRequest request,
        string queryId,
        string processedQuery,
        List<NLWebResult> resultsList,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[StreamingResponses] Yielding initial response for QueryId={QueryId}", queryId);
        var initialResponse = new NLWebResponse
        {
            QueryId = queryId,
            Results = resultsList,
            ProcessedQuery = processedQuery,
            TotalResults = resultsList.Count,
            ProcessingTimeMs = 0 // Will be updated in final response
        };

        yield return initialResponse;

        if (request.Mode == QueryMode.List)
        {
            _logger.LogDebug("[StreamingResponses] List mode complete for QueryId={QueryId}", queryId);
            yield break;
        }

        var contentChunks = new List<string>();

        _logger.LogDebug("[StreamingResponses] Starting streaming content for QueryId={QueryId}, Mode={Mode}", queryId, request.Mode);
        await foreach (var chunk in _resultGenerator.GenerateStreamingResponseAsync(processedQuery, resultsList, request.Mode, cancellationToken))
        {
            contentChunks.Add(chunk);

            var streamingResponse = new NLWebResponse
            {
                QueryId = queryId,
                Results = resultsList,
                ProcessedQuery = processedQuery,
                TotalResults = resultsList.Count,
                Summary = request.Mode == QueryMode.Summarize ? string.Join("", contentChunks) : null,
                GeneratedResponse = request.Mode == QueryMode.Generate ? string.Join("", contentChunks) : null,
                ProcessingTimeMs = 0,
                IsStreaming = true,
                IsComplete = false
            };

            _logger.LogTrace("[StreamingResponses] Yielding chunk for QueryId={QueryId}, ChunkLength={ChunkLength}", queryId, chunk?.Length ?? 0);
            yield return streamingResponse;
        }

        var finalResponse = new NLWebResponse
        {
            QueryId = queryId,
            Results = resultsList,
            ProcessedQuery = processedQuery,
            TotalResults = resultsList.Count,
            Summary = request.Mode == QueryMode.Summarize ? string.Join("", contentChunks) : null,
            GeneratedResponse = request.Mode == QueryMode.Generate ? string.Join("", contentChunks) : null,
            ProcessingTimeMs = 0,
            IsStreaming = false,
            IsComplete = true
        };

        _logger.LogDebug("[StreamingResponses] Yielding final response for QueryId={QueryId}", queryId);
        yield return finalResponse;

        _logger.LogInformation("Successfully processed streaming request {QueryId} with {ResultCount} results", queryId, resultsList.Count);
    }

    /// <summary>
    /// Generates response based on the specified query mode.
    /// </summary>
    private async Task<NLWebResponse> GenerateResponseByModeAsync(
        QueryMode mode,
        string processedQuery,
        List<NLWebResult> results,
        string queryId,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogDebug("[GenerateResponseByModeAsync] Entry for QueryId={QueryId}, Mode={Mode}", queryId, mode);

        var response = new NLWebResponse
        {
            QueryId = queryId,
            Results = results,
            ProcessedQuery = processedQuery,
            TotalResults = results.Count
        };

        switch (mode)
        {
            case QueryMode.List:
                _logger.LogDebug("[GenerateResponseByModeAsync] List mode for QueryId={QueryId}", queryId);
                break;

            case QueryMode.Summarize:
                _logger.LogDebug("[GenerateResponseByModeAsync] Calling GenerateSummaryAsync for QueryId={QueryId}", queryId);
                var (summary, _) = await _resultGenerator.GenerateSummaryAsync(processedQuery, results, cancellationToken);
                response.Summary = summary;
                _logger.LogDebug("[GenerateResponseByModeAsync] GenerateSummaryAsync complete for QueryId={QueryId}", queryId);
                break;

            case QueryMode.Generate:
                _logger.LogDebug("[GenerateResponseByModeAsync] Calling GenerateResponseAsync for QueryId={QueryId}", queryId);
                var (generatedResponse, _) = await _resultGenerator.GenerateResponseAsync(processedQuery, results, cancellationToken);
                response.GeneratedResponse = generatedResponse;
                _logger.LogDebug("[GenerateResponseByModeAsync] GenerateResponseAsync complete for QueryId={QueryId}", queryId);
                break;

            default:
                _logger.LogWarning("Unknown query mode: {Mode}, defaulting to List", mode);
                break;
        }

        response.ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        _logger.LogDebug("[GenerateResponseByModeAsync] Exit for QueryId={QueryId}", queryId);
        return response;
    }

    /// <summary>
    /// Creates an error response with the specified message.
    /// </summary>
    private static NLWebResponse CreateErrorResponse(string errorMessage, string? queryId = null)
    {
        return new NLWebResponse
        {
            QueryId = queryId ?? Guid.NewGuid().ToString(),
            Results = Array.Empty<NLWebResult>(),
            TotalResults = 0,
            Error = errorMessage,
            ProcessingTimeMs = 0
        };
    }
}
