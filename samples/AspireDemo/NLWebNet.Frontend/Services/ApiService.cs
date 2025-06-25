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
            var queryParams = new List<string> { $"query={Uri.EscapeDataString(query)}" };
            
            if (threshold.HasValue)
                queryParams.Add($"threshold={threshold.Value}");
                
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            
            var queryString = string.Join("&", queryParams);
            var requestUrl = $"/api/search?{queryString}";
            
            _logger.LogInformation("Making search request to: {RequestUrl}", requestUrl);
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            if (!string.IsNullOrEmpty(githubToken))
            {
                request.Headers.Add("X-GitHub-Token", githubToken);
                _logger.LogInformation("Added GitHub token to request headers");
            }

            var response = await _httpClient.SendAsync(request);
            
            _logger.LogInformation("Search API response: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var results = await response.Content.ReadFromJsonAsync<ApiSearchResult[]>();
                var resultCount = results?.Length ?? 0;
                _logger.LogInformation("Search API returned {ResultCount} results", resultCount);
                return results ?? Array.Empty<ApiSearchResult>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Search API returned {StatusCode}: {ReasonPhrase}. Content: {Content}", 
                    response.StatusCode, response.ReasonPhrase, errorContent);
                return Array.Empty<ApiSearchResult>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling search API with query: {Query}", query);
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
