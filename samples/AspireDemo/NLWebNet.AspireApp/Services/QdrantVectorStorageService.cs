using NLWebNet.AspireApp.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Grpc.Core;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Qdrant-based implementation of vector storage service 
/// </summary>
public class QdrantVectorStorageService : IVectorStorageService
{
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantVectorStorageService> _logger;
    private const string CollectionName = "nlwebnet_documents";
    private const uint VectorSize = 1536; // OpenAI text-embedding-ada-002 size
    private bool _isInitialized = false;

    public QdrantVectorStorageService(QdrantClient qdrantClient, ILogger<QdrantVectorStorageService> logger)
    {
        _qdrantClient = qdrantClient ?? throw new ArgumentNullException(nameof(qdrantClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if collection exists by trying to get its info
            try 
            {
                await _qdrantClient.GetCollectionInfoAsync(CollectionName, cancellationToken);
                _logger.LogInformation("Qdrant collection already exists: {CollectionName}", CollectionName);
                _isInitialized = true;
                return;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // Collection doesn't exist, create it
                _logger.LogInformation("Collection {CollectionName} doesn't exist, creating it...", CollectionName);
            }

            // Create collection with vector configuration
            await _qdrantClient.CreateCollectionAsync(
                collectionName: CollectionName,
                vectorsConfig: new VectorParams
                {
                    Size = VectorSize,
                    Distance = Distance.Cosine // Use cosine similarity for semantic search
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Created Qdrant collection: {CollectionName} with vector size: {VectorSize}", 
                CollectionName, VectorSize);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Qdrant collection");
            throw;
        }
    }

    public async Task<string> StoreDocumentAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        try
        {
            // Generate a unique ID if not provided
            if (string.IsNullOrEmpty(document.Id))
            {
                document.Id = Guid.NewGuid().ToString();
            }

            // Convert ReadOnlyMemory<float> to float array for Qdrant
            var embeddingArray = document.Embedding.ToArray();
            
            var point = new PointStruct
            {
                Id = new PointId { Uuid = document.Id },
                Vectors = embeddingArray,
                Payload =
                {
                    ["url"] = document.Url,
                    ["title"] = document.Title,
                    ["site"] = document.Site,
                    ["description"] = document.Description,
                    ["score"] = document.Score,
                    ["ingested_at"] = document.IngestedAt.ToString("O"),
                    ["source_type"] = document.SourceType
                }
            };

            var response = await _qdrantClient.UpsertAsync(
                collectionName: CollectionName,
                points: new List<PointStruct> { point },
                cancellationToken: cancellationToken);

            if (response.Status == UpdateStatus.Completed)
            {
                _logger.LogDebug("Stored document with ID: {DocumentId}, Title: {Title}", document.Id, document.Title);
                return document.Id;
            }
            else
            {
                throw new InvalidOperationException($"Failed to store document. Status: {response.Status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document in Qdrant");
            throw;
        }
    }

    public async Task<IEnumerable<(DocumentRecord Document, float Score)>> SearchSimilarAsync(
        ReadOnlyMemory<float> queryEmbedding, 
        int limit = 10, 
        float threshold = 0.7f, 
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        try
        {
            var embeddingArray = queryEmbedding.ToArray();
            
            var searchResponse = await _qdrantClient.SearchAsync(
                collectionName: CollectionName,
                vector: embeddingArray,
                limit: (ulong)limit,
                scoreThreshold: threshold,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var results = new List<(DocumentRecord Document, float Score)>();

            foreach (var point in searchResponse)
            {
                var document = new DocumentRecord
                {
                    Id = point.Id.Uuid,
                    Url = point.Payload["url"].StringValue,
                    Title = point.Payload["title"].StringValue,
                    Site = point.Payload["site"].StringValue,
                    Description = point.Payload["description"].StringValue,
                    Score = (float)point.Payload["score"].DoubleValue,
                    IngestedAt = DateTimeOffset.Parse(point.Payload["ingested_at"].StringValue),
                    SourceType = point.Payload["source_type"].StringValue
                };

                results.Add((document, point.Score));
            }

            _logger.LogDebug("Found {ResultCount} similar documents for search query", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search similar documents in Qdrant");
            throw;
        }
    }

    public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        try
        {
            var collectionInfo = await _qdrantClient.GetCollectionInfoAsync(CollectionName, cancellationToken);
            return (int)collectionInfo.PointsCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document count from Qdrant");
            return 0;
        }
    }

    public async Task<IEnumerable<DocumentRecord>> GetAllDocumentsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        try
        {
            // Use ScrollAsync with collection name and scroll parameters
            var response = await _qdrantClient.ScrollAsync(
                collectionName: CollectionName,
                limit: (uint)limit,
                payloadSelector: true,
                vectorsSelector: false,
                cancellationToken: cancellationToken);

            var documents = new List<DocumentRecord>();
            foreach (var point in response.Result)
            {
                var document = new DocumentRecord
                {
                    Id = point.Id.Uuid,
                    Url = point.Payload["url"].StringValue,
                    Title = point.Payload["title"].StringValue,
                    Site = point.Payload["site"].StringValue,
                    Description = point.Payload["description"].StringValue,
                    Score = (float)point.Payload["score"].DoubleValue,
                    IngestedAt = DateTimeOffset.Parse(point.Payload["ingested_at"].StringValue),
                    SourceType = point.Payload["source_type"].StringValue
                };
                documents.Add(document);
            }

            _logger.LogDebug("Retrieved {Count} documents from Qdrant", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all documents from Qdrant");
            return new List<DocumentRecord>();
        }
    }

    public async Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        try
        {
            // Delete the collection and recreate it
            await _qdrantClient.DeleteCollectionAsync(CollectionName, cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted Qdrant collection: {CollectionName}", CollectionName);
            
            // Recreate the collection
            _isInitialized = false;
            await InitializeAsync(cancellationToken);
            
            _logger.LogInformation("Cleared all documents from Qdrant collection: {CollectionName}", CollectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear documents from Qdrant");
            throw;
        }
    }
}
