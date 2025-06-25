using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Text.Json;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// GitHub Models embedding service implementation
/// </summary>
public class GitHubModelsEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<GitHubModelsEmbeddingService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public GitHubModelsEmbeddingService(
        HttpClient httpClient, 
        string model,
        ILogger<GitHubModelsEmbeddingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await GenerateEmbeddingAsync(text, null, cancellationToken);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, string? githubToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or whitespace", nameof(text));
            }

            _logger.LogDebug("Generating embedding for text with length: {Length} using model: {Model}", text.Length, _model);

            var request = new
            {
                input = text,
                model = _model
            };

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            // Create a new HttpClient instance with the provided token if needed
            var httpClient = _httpClient;
            if (!string.IsNullOrEmpty(githubToken))
            {
                httpClient = new HttpClient();
                httpClient.BaseAddress = _httpClient.BaseAddress;
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "NLWebNet-AspireDemo");
            }

            try
            {
                _logger.LogDebug("POST v1/embeddings to {BaseAddress}", httpClient.BaseAddress);

                var response = await httpClient.PostAsync("v1/embeddings", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, JsonOptions);

                if (embeddingResponse?.Data?.FirstOrDefault()?.Embedding is { } embedding && embedding.Length > 0)
                {
                    _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);
                    return new ReadOnlyMemory<float>(embedding);
                }
                else
                {
                    throw new InvalidOperationException("Failed to generate embedding - empty result from GitHub Models");
                }
            }
            finally
            {
                if (httpClient != _httpClient)
                {
                    httpClient.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text with length: {Length}", text.Length);
            throw;
        }
    }

    // Response models for GitHub Models API
    private class EmbeddingResponse
    {
        public string? Object { get; set; }
        public EmbeddingData[]? Data { get; set; }
        public string? Model { get; set; }
        public Usage? Usage { get; set; }
    }

    private class EmbeddingData
    {
        public string? Object { get; set; }
        public float[]? Embedding { get; set; }
        public int Index { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
