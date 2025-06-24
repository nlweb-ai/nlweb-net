using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;
using NLWebNet.Models;

namespace NLWebNet.Demo.Services;

/// <summary>
/// Blog post data model for RSS feed items.
/// </summary>
public class BlogPost
{
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime PublishedDate { get; set; }
}

/// <summary>
/// Interface for RSS feed service operations.
/// </summary>
public interface IRssFeedService
{
    Task<List<BlogPost>> GetLatestPostsAsync(int count = 10);
    Task<IEnumerable<NLWebResult>> GetFeedItemsAsync(string feedUrl, int maxItems = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for fetching and parsing RSS feeds to provide dynamic content.
/// </summary>
public class RssFeedService : IRssFeedService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssFeedService> _logger;

    public RssFeedService(HttpClient httpClient, ILogger<RssFeedService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<NLWebResult>> GetFeedItemsAsync(string feedUrl, int maxItems = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching RSS feed from: {FeedUrl}", feedUrl);

            var response = await _httpClient.GetStringAsync(feedUrl, cancellationToken);

            using var stringReader = new StringReader(response);
            using var xmlReader = XmlReader.Create(stringReader);

            var feed = SyndicationFeed.Load(xmlReader);
            var results = new List<NLWebResult>();

            foreach (var item in feed.Items.Take(maxItems))
            {
                var result = new NLWebResult
                {
                    Url = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Name = item.Title?.Text ?? "Untitled",
                    Site = ExtractDomain(item.Links.FirstOrDefault()?.Uri.ToString() ?? feedUrl),
                    Score = 0,
                    Description = item.Summary?.Text ?? "",
                    SchemaObject = JsonSerializer.SerializeToElement(new
                    {
                        type = "BlogPosting",
                        headline = item.Title?.Text,
                        datePublished = item.PublishDate.ToString("yyyy-MM-dd"),
                        author = item.Authors.FirstOrDefault()?.Name,
                        url = item.Links.FirstOrDefault()?.Uri.ToString(),
                        description = item.Summary?.Text
                    })
                };

                results.Add(result);
            }

            _logger.LogInformation("Successfully parsed {ItemCount} items from RSS feed", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching RSS feed from {FeedUrl}", feedUrl);
            return Enumerable.Empty<NLWebResult>();
        }
    }

    public async Task<List<BlogPost>> GetLatestPostsAsync(int count = 10)
    {
        try
        {
            _logger.LogInformation("Fetching latest blog posts, count: {Count}", count);

            const string dotnetBlogFeedUrl = "https://devblogs.microsoft.com/dotnet/feed/";
            var response = await _httpClient.GetStringAsync(dotnetBlogFeedUrl);

            using var stringReader = new StringReader(response);
            using var xmlReader = XmlReader.Create(stringReader);

            var feed = SyndicationFeed.Load(xmlReader);
            var posts = new List<BlogPost>();

            foreach (var item in feed.Items.Take(count))
            {
                posts.Add(new BlogPost
                {
                    Title = item.Title?.Text ?? "Untitled",
                    Summary = item.Summary?.Text ?? item.Content?.ToString() ?? "",
                    Url = item.Links?.FirstOrDefault()?.Uri?.ToString() ?? "",
                    PublishedDate = item.PublishDate.DateTime
                });
            }

            _logger.LogInformation("Successfully fetched {Count} blog posts", posts.Count);
            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest blog posts");
            return new List<BlogPost>();
        }
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "unknown.com";
        }
    }
}
