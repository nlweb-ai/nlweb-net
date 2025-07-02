using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NLWebNet.Services;

/// <summary>
/// Implementation of query processing and decontextualization logic.
/// </summary>
public class QueryProcessor : IQueryProcessor
{
    private readonly ILogger<QueryProcessor> _logger;
    private readonly IToolSelector? _toolSelector;

    public QueryProcessor(ILogger<QueryProcessor> logger, IToolSelector? toolSelector = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolSelector = toolSelector;
    }

    /// <inheritdoc />
    public async Task<string> ProcessQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Processing query for request {QueryId}", request.QueryId);

        // Perform tool selection if available and enabled
        if (_toolSelector != null && _toolSelector.ShouldSelectTool(request))
        {
            var selectedTool = await _toolSelector.SelectToolAsync(request, cancellationToken);
            if (!string.IsNullOrEmpty(selectedTool))
            {
                _logger.LogDebug("Tool selection complete: {Tool} for request {QueryId}", selectedTool, request.QueryId);
                // Store the selected tool in the request for downstream processing
                // Note: This is a minimal implementation - the tool selection result could be used
                // by other components in the pipeline
            }
        }

        // If decontextualized query is already provided, use it
        if (!string.IsNullOrEmpty(request.DecontextualizedQuery))
        {
            _logger.LogDebug("Using provided decontextualized query");
            return request.DecontextualizedQuery;
        }

        // If no previous context, return the current query as-is
        if (string.IsNullOrEmpty(request.Prev))
        {
            _logger.LogDebug("No previous context, using current query");
            return request.Query;
        }

        // Perform decontextualization based on previous queries
        var decontextualizedQuery = await PerformDecontextualizationAsync(request.Query, request.Prev, cancellationToken);

        _logger.LogDebug("Decontextualized query: {Query}", decontextualizedQuery);
        return decontextualizedQuery;
    }

    /// <inheritdoc />
    public string GenerateQueryId(NLWebRequest request)
    {
        if (!string.IsNullOrEmpty(request.QueryId))
        {
            return request.QueryId;
        }

        // Generate a unique ID based on timestamp and a short hash
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryHash = Math.Abs(request.Query.GetHashCode()).ToString("X8");
        var queryId = $"{timestamp}-{queryHash}";

        _logger.LogDebug("Generated query ID: {QueryId}", queryId);
        return queryId;
    }

    /// <inheritdoc />
    public bool ValidateRequest(NLWebRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("Request is null");
            return false;
        }

        // Validate using data annotations
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            foreach (var error in validationResults)
            {
                _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
            }
            return false;
        }

        // Additional business logic validation
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            _logger.LogWarning("Query is required but was empty or whitespace");
            return false;
        }

        if (request.Query.Length > 1000) // Arbitrary limit for demo
        {
            _logger.LogWarning("Query exceeds maximum length of 1000 characters");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Performs decontextualization by combining current query with previous context.
    /// This is a simplified implementation - in production, this might use an LLM.
    /// </summary>
    private async Task<string> PerformDecontextualizationAsync(string currentQuery, string previousQueries, CancellationToken cancellationToken)
    {
        // For now, implement a simple rule-based decontextualization
        // In production, this would likely call an LLM service

        await Task.Delay(1, cancellationToken); // Simulate async work

        var prevQueries = previousQueries.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(q => q.Trim())
            .Where(q => !string.IsNullOrEmpty(q))
            .ToList();

        if (!prevQueries.Any())
        {
            return currentQuery;
        }

        // Simple heuristic: if current query contains pronouns or references, 
        // try to expand it with context from previous queries
        var pronouns = new[] { "it", "this", "that", "they", "them", "these", "those" };
        var references = new[] { "above", "mentioned", "previous", "earlier", "before" };

        var currentLower = currentQuery.ToLowerInvariant();
        var needsContext = pronouns.Any(p => currentLower.Contains(p)) ||
                          references.Any(r => currentLower.Contains(r));

        if (!needsContext)
        {
            return currentQuery;
        }

        // For demo purposes, simply prepend the most recent previous query
        var mostRecentQuery = prevQueries.LastOrDefault();
        if (!string.IsNullOrEmpty(mostRecentQuery))
        {
            return $"{mostRecentQuery}. {currentQuery}";
        }

        return currentQuery;
    }
}
