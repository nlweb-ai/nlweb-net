using System.Text.Json.Serialization;

namespace NLWebNet.Models;

/// <summary>
/// Defines the different query modes supported by the NLWeb protocol.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryMode
{
    /// <summary>
    /// Returns the list of top matches from the backend that are most relevant to the query.
    /// </summary>
    List,

    /// <summary>
    /// Summarizes the list and presents the summary along with the list of results.
    /// </summary>
    Summarize,

    /// <summary>
    /// Much more like traditional RAG, where the list is generated and one or more calls 
    /// are made to an LLM to try to answer the user's question.
    /// </summary>
    Generate
}
