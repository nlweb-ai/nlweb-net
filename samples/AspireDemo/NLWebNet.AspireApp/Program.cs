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
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minutes for RSS feeds fetching and processing
});

// Register Qdrant client using Aspire integration
builder.AddQdrantClient("qdrant");

// Register our custom services
builder.Services.AddScoped<IVectorStorageService, QdrantVectorStorageService>();
builder.Services.AddScoped<IRssFeedIngestionService, RssFeedIngestionService>();

// Configure composite embedding service that dynamically selects based on GitHub token
builder.Services.AddHttpClient("GitHubModels"); // Named HttpClient for GitHub Models
builder.Services.AddScoped<IEmbeddingService, CompositeEmbeddingService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS
app.UseCors();

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

app.MapPost("/rss/ingest-demo", async (HttpContext context, IRssFeedIngestionService ingestionService) =>
{
    try
    {
        // Extract GitHub token from headers if provided for consistent embedding
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        
        var count = await ingestionService.IngestDemoFeedsAsync(githubToken);
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
        new { Name = "Microsoft .NET Blog", Url = "https://devblogs.microsoft.com/dotnet/feed/", Note = "Latest 25 articles" }
    };
    
    return Results.Ok(new { 
        Message = "Demo RSS feed used for focused ingestion (latest 25 articles from .NET blog)",
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
    using var activity = System.Diagnostics.Activity.Current?.Source.StartActivity("VectorSearch.SearchDocuments");
    var correlationId = Guid.NewGuid().ToString("N")[..8];
    
    activity?.SetTag("search.correlation_id", correlationId);
    activity?.SetTag("search.query", query);
    activity?.SetTag("search.limit", limit);
    activity?.SetTag("search.threshold", threshold);
    
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("[{CorrelationId}] Search request rejected - empty query", correlationId);
            activity?.SetTag("error", "empty_query");
            return Results.BadRequest(new { Error = "Query parameter is required" });
        }

        var searchLimit = limit ?? 10;
        
        // Extract GitHub token from headers if provided
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        var hasToken = !string.IsNullOrEmpty(githubToken);
        
        // Adjust threshold based on embedding type
        var searchThreshold = threshold ?? (hasToken && IsValidGitHubToken(githubToken) ? 0.1f : 0.03f);
        
        logger.LogInformation("=== SEARCH REQUEST START [{CorrelationId}] ===", correlationId);
        logger.LogInformation("[{CorrelationId}] Search parameters - Query: '{Query}', Limit: {Limit}, Threshold: {Threshold}, HasToken: {HasToken}, TokenLength: {TokenLength}", 
            correlationId, query, searchLimit, searchThreshold, hasToken, githubToken?.Length ?? 0);
        
        activity?.SetTag("auth.has_token", hasToken);
        activity?.SetTag("auth.token_length", githubToken?.Length ?? 0);
        activity?.SetTag("search.processed_limit", searchLimit);
        activity?.SetTag("search.processed_threshold", searchThreshold);
        
        // Generate embedding for the search query
        logger.LogInformation("[{CorrelationId}] Generating query embedding...", correlationId);
        var embeddingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query, githubToken);
        
        embeddingStopwatch.Stop();
        logger.LogInformation("[{CorrelationId}] Query embedding generated - Duration: {Duration}ms, Dimensions: {Dimensions}, EmbeddingType: {EmbeddingType}", 
            correlationId, embeddingStopwatch.ElapsedMilliseconds, queryEmbedding.Length, hasToken ? "GitHub Models" : "Simple Hash");
        
        activity?.SetTag("embedding.duration_ms", embeddingStopwatch.ElapsedMilliseconds);
        activity?.SetTag("embedding.dimensions", queryEmbedding.Length);
        activity?.SetTag("embedding.type", hasToken ? "github_models" : "simple_hash");
        
        // Search for similar documents
        logger.LogInformation("[{CorrelationId}] Performing vector similarity search...", correlationId);
        var searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var results = await vectorStorage.SearchSimilarAsync(queryEmbedding, searchLimit, searchThreshold);
        
        searchStopwatch.Stop();
        var rawResultCount = results.Count();
        
        logger.LogInformation("[{CorrelationId}] Vector search completed - Duration: {Duration}ms, RawResults: {RawResultCount}", 
            correlationId, searchStopwatch.ElapsedMilliseconds, rawResultCount);
        
        activity?.SetTag("vector_search.duration_ms", searchStopwatch.ElapsedMilliseconds);
        activity?.SetTag("vector_search.raw_result_count", rawResultCount);
        
        // Process and format results
        logger.LogInformation("[{CorrelationId}] Processing search results...", correlationId);
        var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var searchResults = results.Select(r => new
        {
            Id = r.Document.Id,
            Title = r.Document.Title,
            Link = r.Document.Url,
            Description = r.Document.Description,
            PublishedDate = r.Document.IngestedAt,
            Similarity = Math.Max(0.0, Math.Min(1.0, r.Score))
        }).ToList();
        
        processingStopwatch.Stop();
        
        // Log result statistics
        if (searchResults.Any())
        {
            var avgSimilarity = searchResults.Average(r => r.Similarity);
            var maxSimilarity = searchResults.Max(r => r.Similarity);
            var minSimilarity = searchResults.Min(r => r.Similarity);
            
            logger.LogInformation("[{CorrelationId}] Result statistics - Count: {Count}, AvgSimilarity: {AvgSimilarity:F3}, MaxSimilarity: {MaxSimilarity:F3}, MinSimilarity: {MinSimilarity:F3}", 
                correlationId, searchResults.Count, avgSimilarity, maxSimilarity, minSimilarity);
            
            logger.LogInformation("[{CorrelationId}] Top result - Title: '{Title}', Similarity: {Similarity:F3}", 
                correlationId, searchResults[0].Title, searchResults[0].Similarity);
            
            activity?.SetTag("results.count", searchResults.Count);
            activity?.SetTag("results.avg_similarity", avgSimilarity);
            activity?.SetTag("results.max_similarity", maxSimilarity);
            activity?.SetTag("results.min_similarity", minSimilarity);
        }
        else
        {
            logger.LogWarning("[{CorrelationId}] No results found for query '{Query}' with threshold {Threshold}", 
                correlationId, query, searchThreshold);
            activity?.SetTag("results.count", 0);
        }

        var totalDuration = embeddingStopwatch.ElapsedMilliseconds + searchStopwatch.ElapsedMilliseconds + processingStopwatch.ElapsedMilliseconds;
        
        logger.LogInformation("=== SEARCH REQUEST SUCCESS [{CorrelationId}] === Total duration: {TotalDuration}ms, Results: {ResultCount}, EmbeddingType: {EmbeddingType}", 
            correlationId, totalDuration, searchResults.Count, hasToken ? "GitHub Models" : "Simple Hash");
        
        activity?.SetTag("search.success", true);
        activity?.SetTag("search.total_duration_ms", totalDuration);
        
        return Results.Ok(searchResults);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "=== SEARCH REQUEST FAILED [{CorrelationId}] === Query: '{Query}', Error: {Message}", correlationId, query, ex.Message);
        
        activity?.SetTag("search.success", false);
        activity?.SetTag("error.type", ex.GetType().Name);
        activity?.SetTag("error.message", ex.Message);
        activity?.SetTag("error.stack_trace", ex.StackTrace);
        
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("SearchDocuments")
.WithOpenApi();

// Diagnostic endpoint for analyzing embeddings and search quality
app.MapGet("/api/diagnostics/embedding", async (HttpContext context, string text, IEmbeddingService embeddingService, ILogger<Program> logger) =>
{
    try
    {
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        var hasToken = !string.IsNullOrEmpty(githubToken) && IsValidGitHubToken(githubToken);
        
        logger.LogInformation("Generating embedding for diagnostic - Text: '{Text}', HasToken: {HasToken}", text, hasToken);
        
        var embedding = await embeddingService.GenerateEmbeddingAsync(text, githubToken);
        
        var stats = new
        {
            Text = text,
            EmbeddingType = hasToken ? "GitHub Models" : "Simple Hash",
            HasGitHubToken = hasToken,
            TokenLength = githubToken?.Length ?? 0,
            EmbeddingDimensions = embedding.Length,
            EmbeddingSample = embedding.Span[0..Math.Min(10, embedding.Length)].ToArray(), // First 10 values
            EmbeddingMagnitude = Math.Sqrt(embedding.Span.ToArray().Sum(x => x * x)),
            EmbeddingStats = new
            {
                Min = embedding.Span.ToArray().Min(),
                Max = embedding.Span.ToArray().Max(),
                Average = embedding.Span.ToArray().Average(),
                NonZeroCount = embedding.Span.ToArray().Count(x => Math.Abs(x) > 0.001f)
            }
        };
        
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating diagnostic embedding for text: {Text}", text);
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("DiagnosticEmbedding")
.WithOpenApi();

// Diagnostic endpoint for searching with detailed analysis
app.MapGet("/api/diagnostics/search", async (HttpContext context, string query, int? limit, IVectorStorageService vectorStorage, IEmbeddingService embeddingService, ILogger<Program> logger) =>
{
    try
    {
        var searchLimit = limit ?? 10;
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        var hasToken = !string.IsNullOrEmpty(githubToken) && IsValidGitHubToken(githubToken);
        
        logger.LogInformation("=== DIAGNOSTIC SEARCH ===");
        logger.LogInformation("Query: '{Query}', HasToken: {HasToken}", query, hasToken);
        
        // Generate query embedding
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query, githubToken);
        
        // Get raw search results with very low threshold
        var results = await vectorStorage.SearchSimilarAsync(queryEmbedding, searchLimit, 0.0f);
        
        var diagnosticResults = results.Select((r, index) => new
        {
            Rank = index + 1,
            Id = r.Document.Id,
            Title = r.Document.Title,
            Description = r.Document.Description?.Substring(0, Math.Min(200, r.Document.Description?.Length ?? 0)) + "...",
            Similarity = r.Score,
            SimilarityPercent = Math.Round(r.Score * 100, 2),
            ContainsQueryTerm = r.Document.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                               r.Document.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true,
            TitleMatch = r.Document.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true,
            DescriptionMatch = r.Document.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
        }).ToList();
        
        var analysis = new
        {
            Query = query,
            EmbeddingType = hasToken ? "GitHub Models" : "Simple Hash",
            HasGitHubToken = hasToken,
            QueryEmbeddingStats = new
            {
                Dimensions = queryEmbedding.Length,
                Magnitude = Math.Sqrt(queryEmbedding.Span.ToArray().Sum(x => x * x)),
                Sample = queryEmbedding.Span[0..Math.Min(5, queryEmbedding.Length)].ToArray()
            },
            TotalResults = diagnosticResults.Count,
            ResultsWithTextMatch = diagnosticResults.Count(r => r.ContainsQueryTerm),
            HighestSimilarity = diagnosticResults.FirstOrDefault()?.Similarity ?? 0,
            LowestSimilarity = diagnosticResults.LastOrDefault()?.Similarity ?? 0,
            Results = diagnosticResults
        };
        
        logger.LogInformation("Diagnostic complete - {ResultCount} results, {TextMatches} contain query term", 
            diagnosticResults.Count, diagnosticResults.Count(r => r.ContainsQueryTerm));
        
        return Results.Ok(analysis);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in diagnostic search for query: {Query}", query);
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("DiagnosticSearch")
.WithOpenApi();

// Diagnostic endpoint to browse ingested documents
app.MapGet("/api/documents", async (IVectorStorageService vectorStorage, string? search = null, int? limit = null) =>
{
    try
    {
        var searchLimit = limit ?? 50;
        var documents = await vectorStorage.GetAllDocumentsAsync(searchLimit);
        
        var results = documents.Select(doc => new
        {
            Id = doc.Id,
            Title = doc.Title,
            Description = doc.Description?.Length > 200 ? doc.Description.Substring(0, 200) + "..." : doc.Description,
            Url = doc.Url,
            IngestedAt = doc.IngestedAt,
            TitleMatch = !string.IsNullOrEmpty(search) && doc.Title.Contains(search, StringComparison.OrdinalIgnoreCase),
            DescriptionMatch = !string.IsNullOrEmpty(search) && !string.IsNullOrEmpty(doc.Description) && doc.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
        }).ToList();
        
        if (!string.IsNullOrEmpty(search))
        {
            // Filter to only documents that contain the search term
            results = results.Where(r => r.TitleMatch || r.DescriptionMatch).ToList();
        }
        
        return Results.Ok(new
        {
            TotalDocuments = documents.Count(),
            SearchTerm = search,
            MatchingDocuments = results.Count,
            Documents = results
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("BrowseDocuments")
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

// Diagnostic endpoint to test embedding consistency
app.MapGet("/api/embedding-test", async (HttpContext context, string text, IEmbeddingService embeddingService, ILogger<Program> logger) =>
{
    try
    {
        // Test both with and without GitHub token
        var simpleEmbedding = await embeddingService.GenerateEmbeddingAsync(text, null);
        var githubEmbedding = await embeddingService.GenerateEmbeddingAsync(text, "dummy_token"); // Will use simple if token is invalid

        // Try with the actual token from headers
        var githubToken = context.Request.Headers["X-GitHub-Token"].FirstOrDefault();
        ReadOnlyMemory<float>? realGithubEmbedding = null;
        
        if (!string.IsNullOrEmpty(githubToken))
        {
            try
            {
                realGithubEmbedding = await embeddingService.GenerateEmbeddingAsync(text, githubToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate embedding with real GitHub token");
            }
        }

        return Results.Ok(new
        {
            Text = text,
            SimpleEmbedding = new
            {
                Dimensions = simpleEmbedding.Length,
                Sample = simpleEmbedding.Span.Slice(0, Math.Min(10, simpleEmbedding.Length)).ToArray(),
                Magnitude = Math.Sqrt(simpleEmbedding.Span.ToArray().Sum(x => x * x))
            },
            GithubEmbedding = new
            {
                Dimensions = githubEmbedding.Length,
                Sample = githubEmbedding.Span.Slice(0, Math.Min(10, githubEmbedding.Length)).ToArray(),
                Magnitude = Math.Sqrt(githubEmbedding.Span.ToArray().Sum(x => x * x))
            },
            RealGithubEmbedding = realGithubEmbedding.HasValue ? new
            {
                Dimensions = realGithubEmbedding.Value.Length,
                Sample = realGithubEmbedding.Value.Span.Slice(0, Math.Min(10, realGithubEmbedding.Value.Length)).ToArray(),
                Magnitude = Math.Sqrt(realGithubEmbedding.Value.Span.ToArray().Sum(x => x * x))
            } : null,
            AreSimpleAndGithubSame = simpleEmbedding.Span.SequenceEqual(githubEmbedding.Span),
            AreGithubEmbeddingsDifferent = realGithubEmbedding.HasValue && !githubEmbedding.Span.SequenceEqual(realGithubEmbedding.Value.Span)
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("TestEmbeddingConsistency")
.WithOpenApi();

app.MapDefaultEndpoints();

// Helper method for GitHub token validation
static bool IsValidGitHubToken(string? token)
{
    return !string.IsNullOrWhiteSpace(token) && 
           (token.StartsWith("gho_") || token.StartsWith("ghp_") || token.StartsWith("github_pat_")) &&
           token.Length > 20;
}

app.Run();
