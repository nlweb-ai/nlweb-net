using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NLWebNet.Models;

/// <summary>
/// Represents a request to the NLWeb protocol endpoints (/ask and /mcp).
/// </summary>
public class NLWebRequest
{
    /// <summary>
    /// The current query in natural language. This is the only required parameter.
    /// </summary>
    [Required]
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// A token corresponding to some subset of the data of a backend. 
    /// For example, a backend might support multiple sites, each with a conversational 
    /// interface restricted to its content. This parameter can be used to specify the site.
    /// </summary>
    [JsonPropertyName("site")]
    public string? Site { get; set; }

    /// <summary>
    /// Comma separated list of previous queries. In most cases, the decontextualized 
    /// query can be constructed from this.
    /// </summary>
    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    /// <summary>
    /// The entire decontextualized query. If this is available, no decontextualization 
    /// is done on the server side.
    /// </summary>
    [JsonPropertyName("decontextualized_query")]
    public string? DecontextualizedQuery { get; set; }

    /// <summary>
    /// Whether to enable streaming responses. Defaults to true.
    /// To turn off streaming, specify a value of false.
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; } = true;

    /// <summary>
    /// Custom query identifier. If none is specified, one will be auto generated.
    /// </summary>
    [JsonPropertyName("query_id")]
    public string? QueryId { get; set; }

    /// <summary>
    /// The query mode. Defaults to List.
    /// </summary>
    [JsonPropertyName("mode")]
    public QueryMode Mode { get; set; } = QueryMode.List;
}
