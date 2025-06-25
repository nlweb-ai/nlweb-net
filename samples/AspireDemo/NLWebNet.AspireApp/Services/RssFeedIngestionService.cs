using NLWebNet.AspireApp.Models;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Net.Http;

namespace NLWebNet.AspireApp.Services;

/// <summary>
/// Interface for RSS feed ingestion service
/// </summary>
public interface IRssFeedIngestionService
{
    Task<int> IngestFeedAsync(string feedUrl, CancellationToken cancellationToken = default);
    Task<int> IngestDemoFeedsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for ingesting RSS feeds and storing documents in vector storage
/// </summary>
public class RssFeedIngestionService : IRssFeedIngestionService
{
    private readonly IVectorStorageService _vectorStorage;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssFeedIngestionService> _logger;

    // Demo RSS feeds for testing - using reliable tech blogs
    private readonly string[] _demoFeeds = new[]
    {
        "https://devblogs.microsoft.com/dotnet/feed/",     // Microsoft .NET Blog
        "https://devblogs.microsoft.com/feed/",           // Main Microsoft Developer Blogs
        "https://blog.jetbrains.com/feed/",               // JetBrains Blog
        "https://stackoverflow.blog/feed/",                // Stack Overflow Blog
        "https://github.blog/feed/"                       // GitHub Blog
    };

    public RssFeedIngestionService(
        IVectorStorageService vectorStorage,
        HttpClient httpClient,
        ILogger<RssFeedIngestionService> logger)
    {
        _vectorStorage = vectorStorage ?? throw new ArgumentNullException(nameof(vectorStorage));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> IngestFeedAsync(string feedUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feedUrl);

        try
        {
            _logger.LogInformation("Starting ingestion of RSS feed: {FeedUrl}", feedUrl);

            // Download the RSS feed with proper headers
            var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
            request.Headers.Add("User-Agent", "NLWebNet RSS Ingestion Service 1.0");
            request.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            _logger.LogInformation("RSS feed response: {StatusCode} for {FeedUrl}", response.StatusCode, feedUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to fetch RSS feed {FeedUrl}. Status: {StatusCode}. Content: {Content}", 
                    feedUrl, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to fetch RSS feed from {feedUrl}. Status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Parse the RSS feed with proper XML settings
            using var stringReader = new StringReader(content);
            var xmlSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore, // Ignore DTD for security while allowing parsing
                XmlResolver = null // Disable external entity resolution for security
            };
            using var xmlReader = XmlReader.Create(stringReader, xmlSettings);
            
            var feed = SyndicationFeed.Load(xmlReader);
            if (feed == null)
            {
                _logger.LogWarning("Failed to parse RSS feed: {FeedUrl}", feedUrl);
                return 0;
            }

            int processedCount = 0;
            var siteName = feed.Title?.Text ?? new Uri(feedUrl).Host;

            // Process each item in the feed
            foreach (var item in feed.Items)
            {
                try
                {
                    var document = CreateDocumentFromFeedItem(item, siteName, feedUrl);
                    if (document != null)
                    {
                        // For demo purposes, generate a simple embedding (in production, use OpenAI or other service)
                        document.Embedding = GenerateSimpleEmbedding(document.Title + " " + document.Description);
                        
                        await _vectorStorage.StoreDocumentAsync(document, cancellationToken);
                        processedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process feed item: {ItemTitle}", item.Title?.Text);
                }
            }

            _logger.LogInformation("Successfully ingested {ProcessedCount} items from feed: {FeedUrl}", 
                processedCount, feedUrl);
            
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest RSS feed: {FeedUrl}", feedUrl);
            throw;
        }
    }

    public async Task<int> IngestDemoFeedsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ingestion of demo RSS feeds");
        
        int totalProcessed = 0;
        var tasks = new List<Task<int>>();

        foreach (var feedUrl in _demoFeeds)
        {
            tasks.Add(IngestFeedAsync(feedUrl, cancellationToken));
        }

        try
        {
            var results = await Task.WhenAll(tasks);
            totalProcessed = results.Sum();
            
            _logger.LogInformation("Successfully ingested {TotalProcessed} items from {FeedCount} demo feeds", 
                totalProcessed, _demoFeeds.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest some demo feeds");
            throw;
        }

        return totalProcessed;
    }

    private static DocumentRecord? CreateDocumentFromFeedItem(SyndicationItem item, string siteName, string feedUrl)
    {
        if (item.Title?.Text == null || item.Links?.FirstOrDefault()?.Uri == null)
        {
            return null;
        }

        return new DocumentRecord
        {
            Id = Guid.NewGuid().ToString(),
            Url = item.Links.First().Uri.ToString(),
            Title = item.Title.Text,
            Site = siteName,
            Description = item.Summary?.Text ?? string.Empty,
            Score = 1.0f, // Default score
            IngestedAt = DateTimeOffset.UtcNow,
            SourceType = "RSS"
        };
    }

    /// <summary>
    /// Generates a simple embedding for demo purposes.
    /// In production, use OpenAI, Azure OpenAI, or another embedding service.
    /// </summary>
    private static ReadOnlyMemory<float> GenerateSimpleEmbedding(string text)
    {
        // Create a simple hash-based embedding for demo purposes
        // This is NOT suitable for production use
        const int embeddingSize = 1536; // OpenAI text-embedding-ada-002 size
        var embedding = new float[embeddingSize];
        
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        for (int i = 0; i < embeddingSize; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range: -1 to 1
        }
        
        // Normalize the embedding vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embeddingSize; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }
        
        return new ReadOnlyMemory<float>(embedding);
    }
}
