using Microsoft.Extensions.AI;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Composite embedding service that dynamically selects between GitHub Models and Simple embeddings
/// based on the provided GitHub token
/// </summary>
public class CompositeEmbeddingService : IEmbeddingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompositeEmbeddingService> _logger;
    private readonly SimpleEmbeddingService _simpleEmbeddingService;

    public CompositeEmbeddingService(
        IServiceProvider serviceProvider,
        ILogger<CompositeEmbeddingService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Always create simple embedding service as fallback
        var simpleLogger = _serviceProvider.GetRequiredService<ILogger<SimpleEmbeddingService>>();
        _simpleEmbeddingService = new SimpleEmbeddingService(simpleLogger);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await GenerateEmbeddingAsync(text, null, cancellationToken);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, string? githubToken, CancellationToken cancellationToken = default)
    {
        // If GitHub token is provided and appears valid, try to use GitHub Models
        if (!string.IsNullOrEmpty(githubToken) && IsValidGitHubToken(githubToken))
        {
            try
            {
                _logger.LogDebug("Attempting to use GitHub Models embedding service with provided token");
                
                var githubService = CreateGitHubModelsService(githubToken);
                var result = await githubService.GenerateEmbeddingAsync(text, githubToken, cancellationToken);
                
                _logger.LogDebug("Successfully generated embedding using GitHub Models");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GitHub Models embedding failed, falling back to simple embeddings");
                // Fall through to simple embeddings
            }
        }
        else
        {
            _logger.LogDebug("No valid GitHub token provided, using simple embeddings");
        }

        // Use simple embeddings as fallback
        return await _simpleEmbeddingService.GenerateEmbeddingAsync(text, githubToken, cancellationToken);
    }

    private GitHubModelsEmbeddingService CreateGitHubModelsService(string githubToken)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("GitHubModels");
        
        // Configure the HttpClient for this request
        httpClient.BaseAddress = new Uri("https://models.inference.ai.azure.com/");
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "NLWebNet-AspireDemo");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        var logger = _serviceProvider.GetRequiredService<ILogger<GitHubModelsEmbeddingService>>();
        return new GitHubModelsEmbeddingService(httpClient, "text-embedding-3-small", logger);
    }

    private static bool IsValidGitHubToken(string token)
    {
        // Basic validation for GitHub token format
        // Real tokens start with 'gho_', 'ghp_', or 'github_pat_'
        return !string.IsNullOrWhiteSpace(token) && 
               (token.StartsWith("gho_") || token.StartsWith("ghp_") || token.StartsWith("github_pat_")) &&
               token.Length > 20; // GitHub tokens are typically much longer
    }
}
