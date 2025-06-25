namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Service for generating semantic embeddings from text
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a semantic embedding for the given text
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding vector</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a semantic embedding for the given text using a specific GitHub token
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <param name="githubToken">GitHub token to use for API access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding vector</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, string? githubToken, CancellationToken cancellationToken = default);
}
