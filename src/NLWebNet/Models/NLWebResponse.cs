using System.Text.Json.Serialization;

namespace NLWebNet.Models;

/// <summary>
/// Represents a response from the NLWeb protocol endpoints (/ask and /mcp).
/// </summary>
public class NLWebResponse
{
    /// <summary>
    /// The unique identifier for this query, either provided in the request or auto-generated.
    /// </summary>
    [JsonPropertyName("query_id")]
    public string QueryId { get; set; } = string.Empty;

    /// <summary>
    /// The original query that was processed.
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// The decontextualized query that was actually processed.
    /// </summary>
    [JsonPropertyName("decontextualized_query")]
    public string? DecontextualizedQuery { get; set; }

    /// <summary>
    /// The query mode that was used for processing.
    /// </summary>
    [JsonPropertyName("mode")]
    public QueryMode Mode { get; set; }

    /// <summary>
    /// The site identifier that was queried.
    /// </summary>
    [JsonPropertyName("site")]
    public string? Site { get; set; }

    /// <summary>
    /// AI-generated summary of the results (available in Summarize and Generate modes).
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Full RAG-generated response (available in Generate mode).
    /// </summary>
    [JsonPropertyName("generated_response")]
    public string? GeneratedResponse { get; set; }

    /// <summary>
    /// Array of search results. The structure will be moved to a richer format 
    /// (starting with schema.org's ItemList) in the future.
    /// </summary>
    [JsonPropertyName("results")]
    public IList<NLWebResult> Results { get; set; } = new List<NLWebResult>();

    /// <summary>
    /// Indicates whether this response is part of a streaming sequence.
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool IsStreaming { get; set; }

    /// <summary>
    /// Indicates whether this is the final message in a streaming sequence.
    /// </summary>
    [JsonPropertyName("is_final")]
    public bool IsFinal { get; set; } = true;

    /// <summary>
    /// Timestamp when the response was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Total number of results found (may be larger than the returned results array).
    /// </summary>
    [JsonPropertyName("total_results")]
    public int? TotalResults { get; set; }    /// <summary>
                                              /// Processing time in milliseconds.
                                              /// </summary>
    [JsonPropertyName("processing_time_ms")]
    public long? ProcessingTimeMs { get; set; }

    /// <summary>
    /// The processed/decontextualized query that was used for search.
    /// </summary>
    [JsonPropertyName("processed_query")]
    public string? ProcessedQuery { get; set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Indicates whether the streaming response is complete.
    /// </summary>
    [JsonPropertyName("is_complete")]
    public bool IsComplete { get; set; } = true;
}
