using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Runtime.CompilerServices;

namespace NLWebNet.Services;

/// <summary>
/// Implementation of result generation using AI services for different query modes.
/// </summary>
public class ResultGenerator : IResultGenerator
{
    private readonly IDataBackend? _dataBackend;
    private readonly IBackendManager? _backendManager;
    private readonly ILogger<ResultGenerator> _logger;
    private readonly NLWebOptions _options;
    private readonly IChatClient? _chatClient;

    /// <summary>
    /// Constructor for single-backend mode (backward compatibility).
    /// </summary>
    public ResultGenerator(
        IDataBackend dataBackend,
        ILogger<ResultGenerator> logger,
        IOptions<NLWebOptions> options,
        IChatClient? chatClient = null)
    {
        _dataBackend = dataBackend ?? throw new ArgumentNullException(nameof(dataBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chatClient = chatClient;

        _logger.LogDebug("ResultGenerator initialized with single backend and ChatClient: {ChatClientType}",
            _chatClient?.GetType().Name ?? "null");
    }

    /// <summary>
    /// Constructor for multi-backend mode.
    /// </summary>
    public ResultGenerator(
        IBackendManager backendManager,
        ILogger<ResultGenerator> logger,
        IOptions<NLWebOptions> options,
        IChatClient? chatClient = null)
    {
        _backendManager = backendManager ?? throw new ArgumentNullException(nameof(backendManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chatClient = chatClient;

        _logger.LogDebug("ResultGenerator initialized with multi-backend manager and ChatClient: {ChatClientType}",
            _chatClient?.GetType().Name ?? "null");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NLWebResult>> GenerateListAsync(string query, string? site = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating list results for query: {Query}", query);

        IEnumerable<NLWebResult> results;
        
        if (_backendManager != null)
        {
            // Use multi-backend manager
            results = await _backendManager.SearchAsync(query, site, _options.MaxResultsPerQuery, cancellationToken);
        }
        else if (_dataBackend != null)
        {
            // Use single backend for backward compatibility
            results = await _dataBackend.SearchAsync(query, site, _options.MaxResultsPerQuery, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Neither backend manager nor single backend is configured");
        }

        _logger.LogDebug("Generated {Count} list results", results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<(string Summary, IEnumerable<NLWebResult> Results)> GenerateSummaryAsync(
        string query,
        IEnumerable<NLWebResult> results,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating summary for query: {Query}", query);

        var resultsList = results.ToList();

        if (!resultsList.Any())
        {
            return ("No results found for your query.", resultsList);
        }

        string summary; if (_chatClient != null)
        {
            _logger.LogDebug("Using AI client {ClientType} to generate summary", _chatClient.GetType().Name);
            // Use AI to generate summary
            summary = await GenerateAISummaryAsync(query, resultsList, cancellationToken);
        }
        else
        {
            _logger.LogDebug("No AI client available, using template-based summary");
            // Fallback to simple template-based summary
            summary = GenerateTemplateSummary(query, resultsList);
        }

        _logger.LogDebug("Generated summary with {Length} characters", summary.Length);
        return (summary, resultsList);
    }

    /// <inheritdoc />
    public async Task<(string GeneratedResponse, IEnumerable<NLWebResult> Results)> GenerateResponseAsync(
        string query,
        IEnumerable<NLWebResult> results,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating AI response for query: {Query}", query);

        var resultsList = results.ToList();

        if (!resultsList.Any())
        {
            return ("I couldn't find any relevant information to answer your question.", resultsList);
        }

        string response; if (_chatClient != null)
        {
            _logger.LogDebug("Using AI client {ClientType} to generate response", _chatClient.GetType().Name);
            // Use AI to generate comprehensive response
            response = await GenerateAIResponseAsync(query, resultsList, cancellationToken);
        }
        else
        {
            _logger.LogDebug("No AI client available, using template-based response");
            // Fallback to template-based response
            response = GenerateTemplateResponse(query, resultsList);
        }

        _logger.LogDebug("Generated response with {Length} characters", response.Length);
        return (response, resultsList);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string query,
        IEnumerable<NLWebResult> results,
        QueryMode mode,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating streaming response for query: {Query}, mode: {Mode}", query, mode);

        var resultsList = results.ToList();

        if (!resultsList.Any())
        {
            yield return "No results found for your query.";
            yield break;
        }

        if (_chatClient != null && mode == QueryMode.Generate)
        {
            // Stream AI-generated response
            await foreach (var chunk in GenerateStreamingAIResponseAsync(query, resultsList, cancellationToken))
            {
                yield return chunk;
            }
        }
        else
        {
            // For non-streaming modes or when AI is not available, return complete response
            var response = mode switch
            {
                QueryMode.List => $"Found {resultsList.Count} results for your query.",
                QueryMode.Summarize => (await GenerateSummaryAsync(query, resultsList, cancellationToken)).Summary,
                QueryMode.Generate => (await GenerateResponseAsync(query, resultsList, cancellationToken)).GeneratedResponse,
                _ => "Invalid query mode specified."
            };

            // Simulate streaming by splitting response into chunks
            var words = response.Split(' ');
            var chunkSize = Math.Max(1, words.Length / 10); // Roughly 10 chunks

            for (int i = 0; i < words.Length; i += chunkSize)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
                if (i + chunkSize < words.Length) chunk += " ";

                yield return chunk;

                // Small delay for more realistic streaming
                await Task.Delay(50, cancellationToken);
            }
        }
    }    /// <summary>
         /// Generates an AI-powered summary using the chat client.
         /// </summary>
    private async Task<string> GenerateAISummaryAsync(string query, List<NLWebResult> results, CancellationToken cancellationToken)
    {
        var context = string.Join("\n\n", results.Take(5).Select(r => $"Title: {r.Name}\nDescription: {r.Description}"));

        var prompt = $"""
            Based on the following search results, provide a concise summary for the user's query: "{query}"

            Search Results:
            {context}

            Please provide a helpful summary that synthesizes the key information from these results.
            """;

        try
        {
            var response = await _chatClient!.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.ToString() ?? GenerateTemplateSummary(query, results);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI summary, falling back to template");
            return GenerateTemplateSummary(query, results);
        }
    }    /// <summary>
         /// Generates an AI-powered response using the chat client.
         /// </summary>
    private async Task<string> GenerateAIResponseAsync(string query, List<NLWebResult> results, CancellationToken cancellationToken)
    {
        var context = string.Join("\n\n", results.Take(5).Select(r => $"Title: {r.Name}\nURL: {r.Url}\nDescription: {r.Description}"));

        var prompt = $"""
            You are a helpful assistant. Based on the following search results, provide a comprehensive answer to the user's question: "{query}"

            Search Results:
            {context}

            Please provide a detailed, accurate response that directly addresses the user's question using the information from these search results. Include specific details and cite relevant sources when appropriate.
            """;

        try
        {
            var response = await _chatClient!.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.ToString() ?? GenerateTemplateResponse(query, results);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI response, falling back to template");
            return GenerateTemplateResponse(query, results);
        }
    }/// <summary>
     /// Generates streaming AI response chunks.
     /// </summary>
    private async IAsyncEnumerable<string> GenerateStreamingAIResponseAsync(
        string query,
        List<NLWebResult> results,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = string.Join("\n\n", results.Take(5).Select(r => $"Title: {r.Name}\nURL: {r.Url}\nDescription: {r.Description}"));

        var prompt = $"""
            You are a helpful assistant. Based on the following search results, provide a comprehensive answer to the user's question: "{query}"

            Search Results:
            {context}

            Please provide a detailed, accurate response that directly addresses the user's question using the information from these search results.
            """;

        IAsyncEnumerable<string>? streamingResponseAsync = null;
        Exception? streamingException = null;

        try
        {
            streamingResponseAsync = GetStreamingResponse(_chatClient!, prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            streamingException = ex;
        }

        if (streamingResponseAsync != null)
        {
            await foreach (var content in streamingResponseAsync)
            {
                yield return content;
            }
        }
        else
        {
            _logger.LogWarning(streamingException, "Failed to generate streaming AI response, falling back to non-streaming");

            // Fallback to non-streaming response split into chunks
            var response = await GenerateAIResponseAsync(query, results, cancellationToken);
            var words = response.Split(' ');
            var chunkSize = 3; // 3 words per chunk

            for (int i = 0; i < words.Length; i += chunkSize)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
                if (i + chunkSize < words.Length) chunk += " ";

                yield return chunk;
                await Task.Delay(100, cancellationToken); // Simulate streaming delay
            }
        }
    }

    /// <summary>
    /// Helper method to handle streaming response that can throw exceptions.
    /// </summary>
    private static async IAsyncEnumerable<string> GetStreamingResponse(
        IChatClient chatClient,
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in chatClient.GetStreamingResponseAsync(prompt, cancellationToken: cancellationToken))
        {
            var content = update.ToString();
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    /// <summary>
    /// Generates a simple template-based summary when AI is not available.
    /// </summary>
    private static string GenerateTemplateSummary(string query, List<NLWebResult> results)
    {
        var topResults = results.Take(3).ToList();
        var summary = $"Found {results.Count} results for '{query}'. ";

        if (topResults.Any())
        {
            summary += "Top results include: " + string.Join(", ", topResults.Select(r => r.Name)) + ".";
        }

        return summary;
    }

    /// <summary>
    /// Generates a simple template-based response when AI is not available.
    /// </summary>
    private static string GenerateTemplateResponse(string query, List<NLWebResult> results)
    {
        var response = $"Based on the search results for '{query}', here's what I found:\n\n";

        foreach (var result in results.Take(3))
        {
            response += $"• **{result.Name}**: {result.Description}\n";
            response += $"  Source: {result.Url}\n\n";
        }

        if (results.Count() > 3)
        {
            response += $"... and {results.Count() - 3} more results.";
        }

        return response;
    }
}
