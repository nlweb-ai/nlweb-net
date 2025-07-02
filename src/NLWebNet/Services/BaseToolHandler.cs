using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;

namespace NLWebNet.Services;

/// <summary>
/// Base implementation for tool handlers providing common functionality.
/// </summary>
public abstract class BaseToolHandler : IToolHandler
{
    protected readonly ILogger Logger;
    protected readonly NLWebOptions Options;
    protected readonly IQueryProcessor QueryProcessor;
    protected readonly IResultGenerator ResultGenerator;

    protected BaseToolHandler(
        ILogger logger,
        IOptions<NLWebOptions> options,
        IQueryProcessor queryProcessor,
        IResultGenerator resultGenerator)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        QueryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        ResultGenerator = resultGenerator ?? throw new ArgumentNullException(nameof(resultGenerator));
    }

    /// <inheritdoc />
    public abstract string ToolType { get; }

    /// <inheritdoc />
    public abstract Task<NLWebResponse> ExecuteAsync(NLWebRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual bool CanHandle(NLWebRequest request)
    {
        // Base implementation - can handle if not null and has a query
        return request?.Query != null && !string.IsNullOrWhiteSpace(request.Query);
    }

    /// <inheritdoc />
    public virtual int GetPriority(NLWebRequest request)
    {
        // Default priority - can be overridden by specific handlers
        return 50;
    }

    /// <summary>
    /// Creates a standard error response for tool execution failures.
    /// </summary>
    /// <param name="request">The original request</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="exception">Optional exception details</param>
    /// <returns>Error response</returns>
    protected NLWebResponse CreateErrorResponse(NLWebRequest request, string errorMessage, Exception? exception = null)
    {
        Logger.LogError(exception, "Tool '{ToolType}' error for request {QueryId}: {ErrorMessage}",
            ToolType, request.QueryId, errorMessage);

        return new NLWebResponse
        {
            QueryId = request.QueryId ?? string.Empty,
            Query = request.Query,
            Mode = request.Mode,
            Results = new List<NLWebResult>
            {
                new NLWebResult
                {
                    Name = "Tool Error",
                    Description = errorMessage,
                    Url = string.Empty,
                    Site = "System",
                    Score = 0.0
                }
            },
            Error = errorMessage,
            ProcessingTimeMs = 0,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a standard success response template.
    /// </summary>
    /// <param name="request">The original request</param>
    /// <param name="results">The results to include</param>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    /// <returns>Success response</returns>
    protected NLWebResponse CreateSuccessResponse(NLWebRequest request, IList<NLWebResult> results, long processingTimeMs)
    {
        return new NLWebResponse
        {
            QueryId = request.QueryId ?? string.Empty,
            Query = request.Query,
            Mode = request.Mode,
            Results = results,
            Error = null, // Success means no error
            ProcessingTimeMs = processingTimeMs,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a tool result with proper property mapping.
    /// </summary>
    /// <param name="name">The name/title of the result</param>
    /// <param name="description">The description/summary of the result</param>
    /// <param name="url">The URL</param>
    /// <param name="site">The site identifier</param>
    /// <param name="score">The relevance score</param>
    /// <returns>A properly formatted NLWebResult</returns>
    protected NLWebResult CreateToolResult(string name, string description, string url = "", string site = "", double score = 1.0)
    {
        return new NLWebResult
        {
            Name = name,
            Description = description,
            Url = url,
            Site = string.IsNullOrEmpty(site) ? ToolType : site,
            Score = score
        };
    }
}