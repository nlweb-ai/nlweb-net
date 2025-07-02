using Microsoft.Extensions.AI;
using OpenAI;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Implementation of embedding service using Microsoft.Extensions.AI
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await GenerateEmbeddingAsync(text, null, cancellationToken);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, string? githubToken, CancellationToken cancellationToken = default)
    {
        // Simple embedding service ignores GitHub token since it doesn't use external APIs
        return await GenerateEmbeddingInternalAsync(text, cancellationToken);
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingInternalAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or whitespace", nameof(text));
            }

            _logger.LogDebug("Generating embedding for text with length: {Length}", text.Length);

            var embeddings = await _embeddingGenerator.GenerateAsync([text], cancellationToken: cancellationToken);
            var embedding = embeddings.FirstOrDefault()?.Vector;

            if (embedding == null || embedding.Value.Length == 0)
            {
                throw new InvalidOperationException("Failed to generate embedding - empty result");
            }

            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Value.Length);
            return embedding.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text");
            throw;
        }
    }
}

/// <summary>
/// Fallback embedding service that generates simple hash-based embeddings for demo purposes
/// </summary>
public class SimpleEmbeddingService : IEmbeddingService
{
    private readonly ILogger<SimpleEmbeddingService> _logger;
    private const int EmbeddingSize = 1536; // Standard OpenAI embedding size

    public SimpleEmbeddingService(ILogger<SimpleEmbeddingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return GenerateEmbeddingAsync(text, null, cancellationToken);
    }

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, string? githubToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or whitespace", nameof(text));
        }

        _logger.LogWarning("Using simple hash-based embedding - not suitable for production semantic search");

        var embedding = GenerateSimpleEmbedding(text);
        return Task.FromResult(embedding);
    }

    /// <summary>
    /// Generates a simple embedding for demo purposes.
    /// In production, use OpenAI, Azure OpenAI, or another embedding service.
    /// </summary>
    private static ReadOnlyMemory<float> GenerateSimpleEmbedding(string text)
    {
        // Create a simple hash-based embedding for demo purposes
        // This is NOT suitable for production use
        var embedding = new float[EmbeddingSize];

        var hash = text.GetHashCode();
        var random = new Random(hash);

        for (int i = 0; i < EmbeddingSize; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range: -1 to 1
        }

        // Normalize the embedding vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < EmbeddingSize; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }

        return new ReadOnlyMemory<float>(embedding);
    }
}
