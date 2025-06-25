using NLWebNet.Frontend.Services;
using System.Net.Http.Json;
using System.Text.Json;

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

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiSearchResult[]> SearchAsync(string query, string? githubToken = null, float? threshold = null, int? limit = null)
    {
        try
        {
            _logger.LogInformation("=== API SERVICE SEARCH START ===");
            _logger.LogInformation("SearchAsync called - Query: '{Query}', HasToken: {HasToken}, Threshold: {Threshold}, Limit: {Limit}", 
                query, !string.IsNullOrEmpty(githubToken), threshold, limit);
            
            var queryParams = new List<string> { $"query={Uri.EscapeDataString(query)}" };
            
            if (threshold.HasValue)
                queryParams.Add($"threshold={threshold.Value}");
                
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            
            var queryString = string.Join("&", queryParams);
            var requestUrl = $"/api/search?{queryString}";
            
            _logger.LogInformation("Making HTTP request to: {RequestUrl}", requestUrl);
            _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress?.ToString() ?? "null");
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            if (!string.IsNullOrEmpty(githubToken))
            {
                request.Headers.Add("X-GitHub-Token", githubToken);
                _logger.LogInformation("Added GitHub token header (length: {TokenLength})", githubToken.Length);
            }
            else
            {
                _logger.LogInformation("No GitHub token provided - using fallback embeddings");
            }

            _logger.LogInformation("Sending HTTP request...");
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogInformation("HTTP Response - StatusCode: {StatusCode}, ReasonPhrase: '{ReasonPhrase}'", 
                response.StatusCode, response.ReasonPhrase);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Reading JSON response...");
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw response content (first 500 chars): {Content}", 
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);
                
                var results = await response.Content.ReadFromJsonAsync<ApiSearchResult[]>();
                var resultCount = results?.Length ?? 0;
                
                _logger.LogInformation("Deserialized {ResultCount} API results", resultCount);
                
                if (results != null)
                {
                    for (int i = 0; i < Math.Min(results.Length, 5); i++) // Log first 5 results
                    {
                        var result = results[i];
                        _logger.LogInformation("API Result {Index}: Title='{Title}', Similarity={Similarity}", 
                            i, result.Title, result.Similarity);
                    }
                }
                
                _logger.LogInformation("=== API SERVICE SEARCH SUCCESS ===");
                return results ?? Array.Empty<ApiSearchResult>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Error - StatusCode: {StatusCode}, ReasonPhrase: '{ReasonPhrase}', Content: {Content}", 
                    response.StatusCode, response.ReasonPhrase, errorContent);
                _logger.LogInformation("=== API SERVICE SEARCH FAILED ===");
                return Array.Empty<ApiSearchResult>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== API SERVICE SEARCH EXCEPTION === Query: '{Query}', Error: {Message}", query, ex.Message);
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
