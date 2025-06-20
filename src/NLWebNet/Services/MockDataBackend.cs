using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using System.Text.Json;

namespace NLWebNet.Services;

/// <summary>
/// Mock implementation of IDataBackend for testing and demo purposes.
/// Provides sample data and basic search functionality.
/// </summary>
public class MockDataBackend : IDataBackend
{
    private readonly ILogger<MockDataBackend> _logger;
    private readonly List<NLWebResult> _sampleData;

    public MockDataBackend(ILogger<MockDataBackend> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleData = GenerateSampleData();
    }    /// <inheritdoc />
    public async Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching for query: {Query}, site: {Site}, maxResults: {MaxResults}", query, site, maxResults);

        await Task.Delay(100, cancellationToken); // Simulate network delay

        // Handle null or empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("Empty or null query provided, returning empty results");
            return Enumerable.Empty<NLWebResult>();
        }

        var queryTerms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 2) // Ignore very short terms
            .ToList();

        var results = _sampleData
            .Where(item => site == null || item.Site == site)
            .Select(item => new
            {
                Item = item,
                Score = CalculateRelevanceScore(item, queryTerms)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .Take(Math.Min(maxResults, 50)) // Cap at 50 for demo
            .Select(result => new NLWebResult
            {
                Url = result.Item.Url,
                Name = result.Item.Name,
                Site = result.Item.Site,
                Score = result.Score,
                Description = result.Item.Description,
                SchemaObject = result.Item.SchemaObject
            })
            .ToList();

        _logger.LogDebug("Found {ResultCount} results for query", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var sites = _sampleData
            .Select(item => item.Site)
            .Where(site => !string.IsNullOrEmpty(site))
            .Distinct()
            .OrderBy(site => site)
            .ToList();

        return sites!;
    }

    /// <inheritdoc />
    public async Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return _sampleData.FirstOrDefault(item =>
            string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public BackendCapabilities GetCapabilities()
    {
        return new BackendCapabilities
        {
            SupportsSiteFiltering = true,
            SupportsFullTextSearch = true,
            SupportsSemanticSearch = false,
            MaxResults = 50,
            Description = "Mock data backend for testing and demonstration purposes"
        };
    }

    /// <summary>
    /// Calculates a relevance score for an item based on query terms.
    /// </summary>
    private static double CalculateRelevanceScore(NLWebResult item, List<string> queryTerms)
    {
        if (!queryTerms.Any()) return 0;

        var searchableText = $"{item.Name} {item.Description}".ToLowerInvariant();
        var score = 0.0;

        foreach (var term in queryTerms)
        {
            // Exact name match gets highest score
            if (item.Name.ToLowerInvariant().Contains(term))
            {
                score += 10.0;
            }
            // Description match gets medium score
            else if (item.Description?.ToLowerInvariant().Contains(term) == true)
            {
                score += 5.0;
            }
        }

        // Boost score for items that match multiple terms
        var matchingTerms = queryTerms.Count(term => searchableText.Contains(term));
        if (matchingTerms > 1)
        {
            score *= (1.0 + 0.2 * (matchingTerms - 1));
        }

        return Math.Round(score, 2);
    }

    /// <summary>
    /// Generates sample data for demonstration purposes.
    /// </summary>
    private static List<NLWebResult> GenerateSampleData()
    {
        return new List<NLWebResult>
        {
            new()
            {
                Url = "https://docs.microsoft.com/dotnet/core/",
                Name = ".NET Core Documentation",
                Site = "docs.microsoft.com",
                Score = 0,
                Description = "Learn about .NET Core, a cross-platform, high-performance, open-source framework for building modern applications.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "documentation", category = "development" })
            },
            new()
            {
                Url = "https://docs.microsoft.com/aspnet/core/",
                Name = "ASP.NET Core Documentation",
                Site = "docs.microsoft.com",
                Score = 0,
                Description = "ASP.NET Core is a cross-platform, high-performance framework for building modern, cloud-based, internet-connected applications.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "documentation", category = "web" })
            },
            new()
            {
                Url = "https://docs.microsoft.com/azure/",
                Name = "Azure Documentation",
                Site = "docs.microsoft.com",
                Score = 0,
                Description = "Learn how to build, deploy, and manage applications and services on Microsoft Azure cloud platform.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "documentation", category = "cloud" })
            },
            new()
            {
                Url = "https://github.com/microsoft/nlweb",
                Name = "NLWeb Protocol Specification",
                Site = "github.com",
                Score = 0,
                Description = "The NLWeb protocol specification for natural language web interfaces and conversational AI.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "specification", category = "ai" })
            },
            new()
            {
                Url = "https://openai.com/blog/chatgpt",
                Name = "ChatGPT: Optimizing Language Models for Dialogue",
                Site = "openai.com",
                Score = 0,
                Description = "ChatGPT is a model which interacts in a conversational way, making it possible to answer follow-up questions.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "blog", category = "ai" })
            },
            new()
            {
                Url = "https://azure.microsoft.com/services/cognitive-services/",
                Name = "Azure Cognitive Services",
                Site = "azure.microsoft.com",
                Score = 0,
                Description = "Add AI capabilities to your applications with Azure Cognitive Services APIs for vision, speech, language, and decision making.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "service", category = "ai" })
            },
            new()
            {
                Url = "https://www.microsoft.com/ai",
                Name = "Microsoft AI Platform",
                Site = "microsoft.com",
                Score = 0,
                Description = "Discover Microsoft's comprehensive AI platform and tools for developers, data scientists, and businesses.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "platform", category = "ai" })
            },
            new()
            {
                Url = "https://docs.microsoft.com/azure/search/",
                Name = "Azure Cognitive Search",
                Site = "docs.microsoft.com",
                Score = 0,
                Description = "Azure Cognitive Search is a cloud search service with AI capabilities for rich search experiences over content.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "service", category = "search" })
            },
            new()
            {
                Url = "https://devblogs.microsoft.com/dotnet/",
                Name = ".NET Developer Blog",
                Site = "devblogs.microsoft.com",
                Score = 0,
                Description = "The latest news, updates, and insights from the .NET development team at Microsoft.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "blog", category = "development" })
            },
            new()
            {
                Url = "https://docs.microsoft.com/graph/",
                Name = "Microsoft Graph Documentation",
                Site = "docs.microsoft.com",
                Score = 0,
                Description = "Microsoft Graph is the gateway to data and intelligence in Microsoft 365, providing a unified API.",
                SchemaObject = JsonSerializer.SerializeToElement(new { type = "documentation", category = "api" })
            }
        };
    }
}
