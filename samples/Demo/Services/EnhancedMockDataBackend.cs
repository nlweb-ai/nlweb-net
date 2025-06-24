using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;
using System.Linq;
using System.Text.Json;

namespace NLWebNet.Demo.Services;

/// <summary>
/// Enhanced data backend with STRICT data source isolation based on LLM availability:
/// 
/// WHEN LLM CONNECTION AVAILABLE (IsAIConfigured = true):
/// 1. .NET queries → ONLY live RSS feed data + minimal mock placeholders
/// 2. General queries → ONLY static science fiction sample data  
/// 
/// WHEN NO LLM CONNECTION (IsAIConfigured = false):
/// 3. All queries → ONLY mock placeholder data (no RSS, no sci-fi static data)
/// 
/// Zero cross-contamination between data sources for clean, predictable results
/// </summary>
public class EnhancedMockDataBackend : IDataBackend
{
    private readonly MockDataBackend _mockBackend;
    private readonly IRssFeedService _rssService;
    private readonly IAIConfigurationService _aiConfigService;
    private readonly ILogger<EnhancedMockDataBackend> _logger;

    // Cache for RSS feed data (simple in-memory cache for demo)
    private readonly Dictionary<string, (DateTime LastFetched, IEnumerable<NLWebResult> Items)> _feedCache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public EnhancedMockDataBackend(
        ILogger<EnhancedMockDataBackend> logger,
        IRssFeedService rssService,
        IAIConfigurationService aiConfigService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rssService = rssService ?? throw new ArgumentNullException(nameof(rssService));
        _aiConfigService = aiConfigService ?? throw new ArgumentNullException(nameof(aiConfigService));

        // Create the underlying mock backend
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<MockDataBackend>();
        _mockBackend = new MockDataBackend(mockLogger);

        _logger.LogInformation("EnhancedMockDataBackend instance created successfully");
    }
    public async Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EnhancedMockDataBackend.SearchAsync called with query: '{Query}', site: {Site}, maxResults: {MaxResults}", query, site, maxResults);

        var queryLower = query.ToLowerInvariant();
        var allResults = new List<NLWebResult>();

        // CHECK LLM AVAILABILITY FIRST - this determines our data strategy
        bool isAIConfigured = _aiConfigService.IsAIConfigured;
        _logger.LogInformation("LLM connection status: {IsConfigured}", isAIConfigured ? "AVAILABLE" : "NOT AVAILABLE");

        if (!isAIConfigured)
        {
            // Strategy 1: NO LLM CONNECTION = ONLY mock placeholder data for all queries
            _logger.LogInformation("No LLM connection - returning ONLY mock placeholder data for all queries");

            var mockResults = GetSimpleMockData().Take(maxResults);
            allResults.AddRange(mockResults);
            _logger.LogInformation("Added {Count} mock placeholder results", allResults.Count);
        }
        else
        {
            // Strategy 2: LLM AVAILABLE = Use real data with strict isolation
            bool isDotNetQuery = queryLower.Contains("blog") || queryLower.Contains("news") || queryLower.Contains("dotnet") ||
                                queryLower.Contains(".net") || queryLower.Contains("release") || queryLower.Contains("update") ||
                                queryLower.Contains("preview") || queryLower.Contains("rc") || queryLower.Contains("announce"); if (isDotNetQuery)
            {
                // Strategy 2a: .NET queries get ONLY RSS feed data (NO mock data)
                _logger.LogInformation("LLM available + .NET query detected - using ONLY RSS feed data");

                var feedItems = await GetCachedFeedItemsAsync(".NET Blog", "https://devblogs.microsoft.com/dotnet/feed/", cancellationToken);
                var feedItemsList = feedItems.ToList();
                allResults.AddRange(feedItemsList);
                _logger.LogInformation("Added {Count} live RSS feed results", feedItemsList.Count);

                // NO mock data for .NET queries - RSS feed data only
                _logger.LogInformation("Skipping mock data for .NET queries - pure RSS feed results only");
            }
            else
            {
                // Strategy 2b: Non-.NET queries get ONLY static sci-fi data (ZERO RSS contamination)
                _logger.LogInformation("LLM available + general query detected - using ONLY static science fiction data");

                var staticResults = await _mockBackend.SearchAsync(query, site, maxResults, cancellationToken);
                var staticResultsList = staticResults.ToList();
                allResults.AddRange(staticResultsList);
                _logger.LogInformation("Added {Count} static science fiction results", staticResultsList.Count);

                // NO mock data for general queries to keep results clean and focused
                _logger.LogInformation("Skipping mock data for general queries to maintain clean sci-fi theme");
            }
        }

        // Apply site filtering if specified
        if (!string.IsNullOrEmpty(site))
        {
            allResults = allResults.Where(r => r.Site == site).ToList();
            _logger.LogInformation("Applied site filter '{Site}', {Count} results remain", site, allResults.Count);
        }

        // VERIFY DATA SOURCE PURITY - ensure no cross-contamination
        var rssFeedCount = allResults.Count(r => r.Site == "devblogs.microsoft.com");
        var mockDataCount = allResults.Count(r => r.Name.StartsWith("[Mock Data]"));
        var staticDataCount = allResults.Count - rssFeedCount - mockDataCount;

        _logger.LogInformation("Data source verification - Static: {StaticCount}, RSS: {RssCount}, Mock: {MockCount}",
            staticDataCount, rssFeedCount, mockDataCount);

        // CRITICAL: Verify data source isolation based on LLM availability
        if (!isAIConfigured && (rssFeedCount > 0 || staticDataCount > 0))
        {
            _logger.LogWarning("DATA CONTAMINATION DETECTED: Found {RssCount} RSS + {StaticCount} static items when LLM unavailable - removing them", rssFeedCount, staticDataCount);
            allResults = allResults.Where(r => r.Name.StartsWith("[Mock Data]")).ToList();
        }
        else if (isAIConfigured)
        {
            bool isDotNetQuery = queryLower.Contains("blog") || queryLower.Contains("news") || queryLower.Contains("dotnet") ||
                                queryLower.Contains(".net") || queryLower.Contains("release") || queryLower.Contains("update") ||
                                queryLower.Contains("preview") || queryLower.Contains("rc") || queryLower.Contains("announce");

            if (!isDotNetQuery && rssFeedCount > 0)
            {
                _logger.LogWarning("DATA CONTAMINATION DETECTED: Found {RssCount} RSS items in non-.NET query - removing them", rssFeedCount);
                allResults = allResults.Where(r => r.Site != "devblogs.microsoft.com").ToList();
            }
            if (isDotNetQuery && (staticDataCount > 0 || mockDataCount > 0))
            {
                _logger.LogWarning("DATA CONTAMINATION DETECTED: Found {StaticCount} static + {MockCount} mock items in .NET query - keeping only RSS",
                    staticDataCount, mockDataCount);
                allResults = allResults.Where(r => r.Site == "devblogs.microsoft.com").ToList();
            }

            if (!isDotNetQuery && mockDataCount > 0)
            {
                _logger.LogWarning("DATA CONTAMINATION DETECTED: Found {MockCount} mock items in general query - removing them", mockDataCount);
                allResults = allResults.Where(r => !r.Name.StartsWith("[Mock Data]")).ToList();
            }
        }

        // Score and rank results (now with pure data sources)
        var scoredResults = ScoreResults(allResults, query);

        // Return top results
        var finalResults = scoredResults
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();

        // Final verification log
        var finalRssFeedCount = finalResults.Count(r => r.Site == "devblogs.microsoft.com");
        var finalMockDataCount = finalResults.Count(r => r.Name.StartsWith("[Mock Data]"));
        var finalStaticDataCount = finalResults.Count - finalRssFeedCount - finalMockDataCount;
        _logger.LogInformation("Final results - Static: {StaticCount}, RSS: {RssCount}, Mock: {MockCount}",
            finalStaticDataCount, finalRssFeedCount, finalMockDataCount);

        _logger.LogInformation("EnhancedMockDataBackend returning {Count} total results", finalResults.Count);
        return finalResults;
    }

    public async Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default)
    {
        var mockSites = await _mockBackend.GetAvailableSitesAsync(cancellationToken);

        // Add RSS feed sites
        var additionalSites = new[] { "devblogs.microsoft.com" };

        return mockSites.Concat(additionalSites).Distinct().OrderBy(s => s);
    }

    public async Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        // First try mock backend
        var result = await _mockBackend.GetItemByUrlAsync(url, cancellationToken);
        if (result != null)
            return result;

        // If not found in mock data, could implement URL fetching here
        // For now, just return null
        return null;
    }

    public BackendCapabilities GetCapabilities()
    {
        return new BackendCapabilities
        {
            SupportsSiteFiltering = true,
            SupportsFullTextSearch = true,
            SupportsSemanticSearch = false,
            MaxResults = 100,
            Description = "Enhanced mock data backend with live RSS feed integration for demonstration"
        };
    }

    private async Task<IEnumerable<NLWebResult>> GetCachedFeedItemsAsync(string feedName, string feedUrl, CancellationToken cancellationToken)
    {
        // Check cache first
        if (_feedCache.TryGetValue(feedName, out var cached) &&
            DateTime.UtcNow - cached.LastFetched < _cacheExpiry)
        {
            _logger.LogDebug("Using cached {FeedName} data", feedName);
            return cached.Items;
        }

        try
        {
            // Fetch fresh data
            _logger.LogInformation("Fetching fresh {FeedName} data from {FeedUrl}", feedName, feedUrl);
            var items = await _rssService.GetFeedItemsAsync(feedUrl, 20, cancellationToken);

            // Update cache
            _feedCache[feedName] = (DateTime.UtcNow, items);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {FeedName}, using cached data if available", feedName);

            // Return cached data even if expired, or empty if no cache
            return _feedCache.TryGetValue(feedName, out var fallback)
                ? fallback.Items
                : Enumerable.Empty<NLWebResult>();
        }
    }

    private static List<NLWebResult> ScoreResults(List<NLWebResult> results, string query)
    {
        var queryTerms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 2)
            .ToList();

        foreach (var result in results)
        {
            var searchableText = $"{result.Name} {result.Description}".ToLowerInvariant();
            var score = 0.0;

            foreach (var term in queryTerms)
            {
                if (result.Name.ToLowerInvariant().Contains(term))
                {
                    score += 10.0;
                }
                else if (result.Description?.ToLowerInvariant().Contains(term) == true)
                {
                    score += 5.0;
                }
            }

            // Boost recent content (if it's from RSS feeds)
            if (result.Site == "devblogs.microsoft.com")
            {
                score += 2.0; // Slight boost for recent blog content
            }

            result.Score = Math.Round(score, 2);
        }

        return results;
    }

    private static IEnumerable<NLWebResult> GetSimpleMockData()
    {
        return new[]
        {
            new NLWebResult
            {
                Url = "https://example.com/mock/1",
                Name = "[Mock Data] Example Item One",
                Site = "example.com",
                Score = 1.0,
                Description = "This is some example mock data to demonstrate placeholder content.",
                SchemaObject = JsonSerializer.SerializeToElement(new
                {
                    type = "Article",
                    name = "Example Item One",
                    description = "Mock placeholder content"
                })
            },
            new NLWebResult
            {
                Url = "https://example.com/mock/2",
                Name = "[Mock Data] Sample Demo Content",
                Site = "example.com",
                Score = 1.0,
                Description = "This is additional mock data used for demonstration purposes only.",
                SchemaObject = JsonSerializer.SerializeToElement(new
                {
                    type = "Article",
                    name = "Sample Demo Content",
                    description = "Mock placeholder content"
                })
            },
            new NLWebResult
            {
                Url = "https://example.com/mock/3",
                Name = "[Mock Data] Placeholder Result",
                Site = "example.com",
                Score = 1.0,
                Description = "This is a placeholder result showing how mock data appears in search results.",
                SchemaObject = JsonSerializer.SerializeToElement(new
                {
                    type = "Article",
                    name = "Placeholder Result",
                    description = "Mock placeholder content"
                })
            }
        };
    }
}
