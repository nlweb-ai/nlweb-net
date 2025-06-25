using NLWebNet.AspireApp.Services;
using Qdrant.Client;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

// Register HttpClient for RSS ingestion
builder.Services.AddHttpClient<IRssFeedIngestionService, RssFeedIngestionService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "NLWebNet RSS Ingestion Service 1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Qdrant client using Aspire integration
builder.AddQdrantClient("qdrant");

// Register our custom services
builder.Services.AddScoped<IVectorStorageService, QdrantVectorStorageService>();
builder.Services.AddScoped<IRssFeedIngestionService, RssFeedIngestionService>();

// Configure embedding service - prioritize GitHub Models, fallback to simple embeddings
var githubToken = builder.Configuration["GitHub:Token"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if (!string.IsNullOrEmpty(githubToken))
{
    // Register GitHub Models embedding service (recommended)
    builder.Services.AddHttpClient<IEmbeddingService, GitHubModelsEmbeddingService>(client =>
    {
        client.BaseAddress = new Uri("https://models.inference.ai.azure.com/");
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddTypedClient<IEmbeddingService>((httpClient, serviceProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<GitHubModelsEmbeddingService>>();
        // Use text-embedding-3-small model for embeddings
        return new GitHubModelsEmbeddingService(httpClient, "text-embedding-3-small", logger);
    });
}
else
{
    // Fallback to simple embedding service for demo purposes
    builder.Services.AddScoped<IEmbeddingService, SimpleEmbeddingService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// API endpoints
app.MapGet("/", () => "NLWebNet Aspire App - Vector Search with Qdrant")
    .WithName("GetRoot")
    .WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("GetHealth")
    .WithOpenApi();

app.MapGet("/api/health", () => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTimeOffset.UtcNow,
    Service = "NLWebNet AspireApp API"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapPost("/rss/ingest", async (string feedUrl, IRssFeedIngestionService ingestionService) =>
{
    try
    {
        var count = await ingestionService.IngestFeedAsync(feedUrl);
        return Results.Ok(new { Message = $"Successfully ingested {count} documents", Count = count });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("IngestRssFeed")
.WithOpenApi();

app.MapPost("/rss/ingest-demo", async (IRssFeedIngestionService ingestionService) =>
{
    try
    {
        var count = await ingestionService.IngestDemoFeedsAsync();
        return Results.Ok(new { Message = $"Successfully ingested {count} documents from demo feeds", Count = count });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("IngestDemoFeeds")
.WithOpenApi();

app.MapGet("/api/demo-feeds", () =>
{
    var demoFeeds = new[]
    {
        new { Name = "Microsoft .NET Blog", Url = "https://devblogs.microsoft.com/dotnet/feed/" },
        new { Name = "Microsoft Developer Blogs", Url = "https://devblogs.microsoft.com/feed/" },
        new { Name = "JetBrains Blog", Url = "https://blog.jetbrains.com/feed/" },
        new { Name = "Stack Overflow Blog", Url = "https://stackoverflow.blog/feed/" },
        new { Name = "GitHub Blog", Url = "https://github.blog/feed/" }
    };
    
    return Results.Ok(new { 
        Message = "Demo RSS feeds used for ingestion",
        Feeds = demoFeeds 
    });
})
.WithName("GetDemoFeeds")
.WithOpenApi();

app.MapGet("/vector/stats", async (IVectorStorageService vectorStorage, ILogger<Program> logger) =>
{
    try
    {
        var count = await vectorStorage.GetDocumentCountAsync();
        logger.LogInformation("Vector storage stats requested: {DocumentCount} documents stored", count);
        return Results.Ok(new { DocumentCount = count, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get vector storage stats");
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("GetVectorStats")
.WithOpenApi();

// Search endpoint
app.MapGet("/api/search", async (HttpContext context, string query, int? limit, float? threshold, IVectorStorageService vectorStorage, IEmbeddingService embeddingService, ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { Error = "Query parameter is required" });
        }

        var searchLimit = limit ?? 10;
        var searchThreshold = threshold ?? 0.1f; // Lower threshold since we're using simple embeddings

        // Extract GitHub token from headers if provided
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        
        logger.LogInformation("Search request: Query='{Query}', Limit={Limit}, Threshold={Threshold}, HasToken={HasToken}", 
            query, searchLimit, searchThreshold, !string.IsNullOrEmpty(githubToken));
        
        // Generate embedding for the search query using the same method as ingestion
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query, githubToken);
        
        logger.LogInformation("Generated query embedding with {Dimensions} dimensions", queryEmbedding.Length);
        
        // Search for similar documents
        var results = await vectorStorage.SearchSimilarAsync(queryEmbedding, searchLimit, searchThreshold);
        
        logger.LogInformation("Vector search returned {RawResultCount} results from storage", results.Count());
        
        var searchResults = results.Select(r => new
        {
            Id = r.Document.Id,
            Title = r.Document.Title,
            Link = r.Document.Url, // Use Link instead of Url to match frontend expectations
            Description = r.Document.Description,
            PublishedDate = r.Document.IngestedAt, // Use the ingestion time as published date
            Similarity = Math.Max(0.0, Math.Min(1.0, r.Score)) // Ensure similarity is between 0 and 1
        }).ToList();

        logger.LogInformation("Search for '{Query}' returned {ResultCount} results (using {TokenType})", 
            query, searchResults.Count, string.IsNullOrEmpty(githubToken) ? "simple embeddings" : "GitHub Models");
        
        return Results.Ok(searchResults);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to search documents");
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("SearchDocuments")
.WithOpenApi();

app.MapDelete("/vector/clear", async (IVectorStorageService vectorStorage) =>
{
    try
    {
        var success = await vectorStorage.ClearAllDocumentsAsync();
        return Results.Ok(new { Message = "All documents cleared", Success = success });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("ClearVectors")
.WithOpenApi();

app.MapDefaultEndpoints();

app.Run();
