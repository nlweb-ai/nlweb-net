namespace NLWebNet;

/// <summary>
/// Configuration options for NLWebNet
/// </summary>
public class NLWebOptions
{
    /// <summary>
    /// Default query mode when none is specified
    /// </summary>
    public QueryMode DefaultMode { get; set; } = QueryMode.List;

    /// <summary>
    /// Whether streaming is enabled by default
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Default timeout for queries in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;
}
