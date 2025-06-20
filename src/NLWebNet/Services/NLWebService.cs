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
    private readonly IDataBackend _dataBackend;
    private readonly ILogger<NLWebService> _logger;
    private readonly NLWebOptions _options;

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

    /// <inheritdoc />
    public async Task<NLWebResponse> ProcessRequestAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing NLWeb request for query: {Query}", request.Query);

        try
        {
            // Validate the request
            if (!_queryProcessor.ValidateRequest(request))
            {
                return CreateErrorResponse("Invalid request. Please check your query and try again.", request.QueryId);
            }

            // Generate query ID if not provided
            var queryId = _queryProcessor.GenerateQueryId(request);

            // Process the query (decontextualization)
            var processedQuery = await _queryProcessor.ProcessQueryAsync(request, cancellationToken);

            // Get initial results from data backend
            var searchResults = await _resultGenerator.GenerateListAsync(processedQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();

            // Generate response based on mode
            var response = await GenerateResponseByModeAsync(request.Mode, processedQuery, resultsList, queryId, cancellationToken);

            _logger.LogInformation("Successfully processed request {QueryId} with {ResultCount} results", queryId, resultsList.Count);
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request processing was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NLWeb request");
            return CreateErrorResponse("An error occurred while processing your request. Please try again.", request.QueryId);
        }
    }    /// <inheritdoc />
    public async IAsyncEnumerable<NLWebResponse> ProcessRequestStreamAsync(
        NLWebRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming NLWeb request for query: {Query}", request.Query);

        // Validate the request
        if (!_queryProcessor.ValidateRequest(request))
        {
            yield return CreateErrorResponse("Invalid request. Please check your query and try again.", request.QueryId);
            yield break;
        }

        // Generate query ID if not provided
        var queryId = _queryProcessor.GenerateQueryId(request);

        // Process the request and yield results, handling exceptions appropriately
        await foreach (var response in ProcessRequestStreamInternalAsync(request, queryId, cancellationToken))
        {
            yield return response;
        }
    }

    /// <summary>
    /// Internal implementation of streaming request processing with proper exception handling.
    /// </summary>
    private async IAsyncEnumerable<NLWebResponse> ProcessRequestStreamInternalAsync(
        NLWebRequest request,
        string queryId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<NLWebResponse>? responseStream = null;
        Exception? processingException = null;

        try
        {
            // Process the query (decontextualization)
            var processedQuery = await _queryProcessor.ProcessQueryAsync(request, cancellationToken);

            // Get initial results from data backend
            var searchResults = await _resultGenerator.GenerateListAsync(processedQuery, request.Site, cancellationToken);
            var resultsList = searchResults.ToList();

            responseStream = GenerateStreamingResponsesAsync(request, queryId, processedQuery, resultsList, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request processing was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
        }

        if (responseStream != null)
        {
            await foreach (var response in responseStream)
            {
                yield return response;
            }
        }
        else
        {
            _logger.LogError(processingException, "Error processing streaming NLWeb request");
            yield return CreateErrorResponse("An error occurred while processing your request. Please try again.", queryId);
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
        // First, yield the basic response structure with results
        var initialResponse = new NLWebResponse
        {
            QueryId = queryId,
            Results = resultsList,
            ProcessedQuery = processedQuery,
            TotalResults = resultsList.Count,
            ProcessingTimeMs = 0 // Will be updated in final response
        };

        yield return initialResponse;

        // For List mode, we're done after sending results
        if (request.Mode == QueryMode.List)
        {
            yield break;
        }

        // For other modes, stream the generated content
        var contentChunks = new List<string>();

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

            yield return streamingResponse;
        }

        // Send final complete response
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
                // For list mode, we just return the results as-is
                break;

            case QueryMode.Summarize:
                var (summary, _) = await _resultGenerator.GenerateSummaryAsync(processedQuery, results, cancellationToken);
                response.Summary = summary;
                break;

            case QueryMode.Generate:
                var (generatedResponse, _) = await _resultGenerator.GenerateResponseAsync(processedQuery, results, cancellationToken);
                response.GeneratedResponse = generatedResponse;
                break;

            default:
                _logger.LogWarning("Unknown query mode: {Mode}, defaulting to List", mode);
                break;
        }

        response.ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
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
