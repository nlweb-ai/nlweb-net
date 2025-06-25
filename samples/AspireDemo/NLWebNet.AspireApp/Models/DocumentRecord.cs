namespace NLWebNet.AspireApp.Models;

/// <summary>
/// Document record for RSS feed content stored in Qdrant vector database.
/// Simple model without VectorData attributes for now (will be added back when Microsoft.Extensions.VectorData is fully supported).
/// </summary>
public class DocumentRecord
{
    /// <summary>
    /// Unique identifier for the document
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Document URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Document title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Site name
    /// </summary>
    public string Site { get; set; } = string.Empty;

    /// <summary>
    /// Document description or summary
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Document relevance score
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// When the document was ingested
    /// </summary>
    public DateTimeOffset IngestedAt { get; set; }

    /// <summary>
    /// Source type (e.g., "RSS", "Web", etc.)
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding for the document
    /// </summary>
    public ReadOnlyMemory<float> Embedding { get; set; }
}
