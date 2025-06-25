using NLWebNet.AspireApp.Models;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Service for storing and retrieving documents using vector embeddings
/// </summary>
public interface IVectorStorageService
{
    /// <summary>
    /// Initialize the vector storage service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Store a document with its vector embedding
    /// </summary>
    /// <param name="document">The document to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the stored document</returns>
    Task<string> StoreDocumentAsync(DocumentRecord document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar documents using vector similarity
    /// </summary>
    /// <param name="queryEmbedding">The query vector embedding</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar documents with their similarity scores</returns>
    Task<IEnumerable<(DocumentRecord Document, float Score)>> SearchSimilarAsync(
        ReadOnlyMemory<float> queryEmbedding, 
        int limit = 10, 
        float threshold = 0.7f, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the total number of stored documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total document count</returns>
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all documents from the vector storage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default);
}
