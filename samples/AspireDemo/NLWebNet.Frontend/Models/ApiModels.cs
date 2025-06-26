namespace NLWebNet.Frontend.Models;

public class VectorStats
{
    public int DocumentCount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RssIngestionRequest
{
    public string FeedUrl { get; set; } = string.Empty;
}

public class RssIngestionResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DocumentRecord
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Score { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
    public string SourceType { get; set; } = string.Empty;
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public float Threshold { get; set; } = 0.7f;
}

public class SearchResult
{
    public DocumentRecord Document { get; set; } = new();
    public float SimilarityScore { get; set; }
}
