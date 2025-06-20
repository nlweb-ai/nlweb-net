namespace NLWebNet;

/// <summary>
/// Represents the different query modes supported by NLWeb
/// </summary>
public enum QueryMode
{
    /// <summary>
    /// Returns the list of top matches from the backend that are most relevant to the query
    /// </summary>
    List,

    /// <summary>
    /// Summarizes the list and presents the summary and also returns the list
    /// </summary>
    Summarize,

    /// <summary>
    /// Much more like traditional RAG, where the list is generated and one or more calls are made to an LLM to try answer the user's question
    /// </summary>
    Generate
}
