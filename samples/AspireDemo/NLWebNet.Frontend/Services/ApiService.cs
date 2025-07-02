using NLWebNet.Frontend.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

namespace NLWebNet.Frontend.Services;

public interface IApiService
{
    Task<ApiSearchResult[]> SearchAsync(string query, string? githubToken = null, float? threshold = null, int? limit = null);
    Task<bool> TestConnectionAsync(string githubToken);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private static readonly ActivitySource ActivitySource = new("NLWebNet.Frontend.ApiService");

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiSearchResult[]> SearchAsync(string query, string? githubToken = null, float? threshold = null, int? limit = null)
    {
        using var activity = ActivitySource.StartActivity("ApiService.SearchAsync");
        activity?.SetTag("search.query", query);
        activity?.SetTag("search.has_token", !string.IsNullOrEmpty(githubToken));
        activity?.SetTag("search.threshold", threshold);
        activity?.SetTag("search.limit", limit);

        var correlationId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogInformation("=== API SERVICE SEARCH START [{CorrelationId}] ===", correlationId);
            _logger.LogInformation("[{CorrelationId}] SearchAsync called - Query: '{Query}', HasToken: {HasToken}, Threshold: {Threshold}, Limit: {Limit}",
                correlationId, query, !string.IsNullOrEmpty(githubToken), threshold, limit);

            var queryParams = new List<string> { $"query={Uri.EscapeDataString(query)}" };

            if (threshold.HasValue)
                queryParams.Add($"threshold={threshold.Value}");

            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");

            var queryString = string.Join("&", queryParams);
            var requestUrl = $"/api/search?{queryString}";

            _logger.LogInformation("[{CorrelationId}] Building HTTP request - URL: {RequestUrl}", correlationId, requestUrl);
            _logger.LogInformation("[{CorrelationId}] HttpClient BaseAddress: {BaseAddress}", correlationId, _httpClient.BaseAddress?.ToString() ?? "null");

            activity?.SetTag("http.url", requestUrl);
            activity?.SetTag("http.base_address", _httpClient.BaseAddress?.ToString());

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            if (!string.IsNullOrEmpty(githubToken))
            {
                request.Headers.Add("X-GitHub-Token", githubToken);
                _logger.LogInformation("[{CorrelationId}] Added GitHub token header (length: {TokenLength})", correlationId, githubToken.Length);
                activity?.SetTag("auth.token_length", githubToken.Length);
            }
            else
            {
                _logger.LogInformation("[{CorrelationId}] No GitHub token provided - using fallback embeddings", correlationId);
            }

            _logger.LogInformation("[{CorrelationId}] Sending HTTP request...", correlationId);
            var httpStopwatch = Stopwatch.StartNew();

            var response = await _httpClient.SendAsync(request);

            httpStopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] HTTP Response received - Duration: {Duration}ms, StatusCode: {StatusCode}, ReasonPhrase: '{ReasonPhrase}'",
                correlationId, httpStopwatch.ElapsedMilliseconds, response.StatusCode, response.ReasonPhrase);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("http.duration_ms", httpStopwatch.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[{CorrelationId}] Reading JSON response...", correlationId);
                var jsonStopwatch = Stopwatch.StartNew();

                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("[{CorrelationId}] Raw response content - Length: {Length} chars, Sample: {Sample}",
                    correlationId, responseContent.Length,
                    responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

                var results = await response.Content.ReadFromJsonAsync<ApiSearchResult[]>();

                jsonStopwatch.Stop();
                var resultCount = results?.Length ?? 0;

                _logger.LogInformation("[{CorrelationId}] JSON deserialization completed - Duration: {Duration}ms, ResultCount: {ResultCount}",
                    correlationId, jsonStopwatch.ElapsedMilliseconds, resultCount);

                activity?.SetTag("response.result_count", resultCount);
                activity?.SetTag("response.json_parse_duration_ms", jsonStopwatch.ElapsedMilliseconds);

                if (results != null && results.Length > 0)
                {
                    _logger.LogInformation("[{CorrelationId}] First result details - Title: '{Title}', Similarity: {Similarity:F3}",
                        correlationId, results[0].Title, results[0].Similarity);

                    activity?.SetTag("response.first_result_similarity", results[0].Similarity);

                    // Log similarity score distribution
                    var highScores = results.Count(r => r.Similarity >= 0.7);
                    var mediumScores = results.Count(r => r.Similarity >= 0.4 && r.Similarity < 0.7);
                    var lowScores = results.Count(r => r.Similarity < 0.4);

                    _logger.LogInformation("[{CorrelationId}] Similarity distribution - High (≥0.7): {High}, Medium (0.4-0.7): {Medium}, Low (<0.4): {Low}",
                        correlationId, highScores, mediumScores, lowScores);

                    activity?.SetTag("results.high_similarity_count", highScores);
                    activity?.SetTag("results.medium_similarity_count", mediumScores);
                    activity?.SetTag("results.low_similarity_count", lowScores);
                }

                _logger.LogInformation("=== API SERVICE SEARCH SUCCESS [{CorrelationId}] === Total duration: {TotalDuration}ms",
                    correlationId, httpStopwatch.ElapsedMilliseconds + jsonStopwatch.ElapsedMilliseconds);

                activity?.SetTag("search.success", true);
                activity?.SetTag("search.total_duration_ms", httpStopwatch.ElapsedMilliseconds + jsonStopwatch.ElapsedMilliseconds);

                return results ?? Array.Empty<ApiSearchResult>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("[{CorrelationId}] API Error - StatusCode: {StatusCode}, ReasonPhrase: '{ReasonPhrase}', Content: {Content}",
                    correlationId, response.StatusCode, response.ReasonPhrase, errorContent);

                activity?.SetTag("search.success", false);
                activity?.SetTag("error.http_status", (int)response.StatusCode);
                activity?.SetTag("error.content", errorContent);

                _logger.LogInformation("=== API SERVICE SEARCH FAILED [{CorrelationId}] ===", correlationId);
                return Array.Empty<ApiSearchResult>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== API SERVICE SEARCH EXCEPTION [{CorrelationId}] === Query: '{Query}', Error: {Message}", correlationId, query, ex.Message);

            activity?.SetTag("search.success", false);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.stack_trace", ex.StackTrace);

            return Array.Empty<ApiSearchResult>();
        }
    }

    public async Task<bool> TestConnectionAsync(string githubToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");

            if (!string.IsNullOrEmpty(githubToken))
            {
                request.Headers.Add("X-GitHub-Token", githubToken);
            }

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API connection");
            return false;
        }
    }
}

public record ApiSearchResult(
    string Title,
    string Description,
    string Link,
    DateTime PublishedDate,
    double Similarity
);
