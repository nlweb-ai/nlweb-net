using NLWebNet.AspireApp.Services;
using Qdrant.Client;

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

app.MapGet("/vector/stats", async (IVectorStorageService vectorStorage) =>
{
    try
    {
        var count = await vectorStorage.GetDocumentCountAsync();
        return Results.Ok(new { DocumentCount = count, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("GetVectorStats")
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
