using NLWebNet.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Configure logging to reduce noise
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    options.AddFilter("Aspire", LogLevel.Warning);
    options.AddFilter("OpenTelemetry", LogLevel.Warning);
    options.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
    options.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    options.AddFilter("Microsoft.Extensions.DependencyInjection", LogLevel.Warning);
    options.AddFilter("System.Net.Http", LogLevel.Warning);
});

// Add Qdrant vector database for storing ingested data
var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume();  // Persist data between container restarts

// Add external dependencies (optional - could be databases, message queues, etc.)
// var postgres = builder.AddPostgres("postgres")
//     .WithEnvironment("POSTGRES_DB", "nlwebnet")
//     .PublishAsAzurePostgresFlexibleServer();

// var redis = builder.AddRedis("redis")
//     .PublishAsAzureRedis();

// Add the NLWebNet Aspire application with Qdrant integration
var nlwebapp = builder.AddProject<Projects.NLWebNet_AspireApp>("nlwebnet-aspire-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("NLWebNet__RateLimiting__RequestsPerWindow", "1000")
    .WithEnvironment("NLWebNet__RateLimiting__WindowSizeInMinutes", "1")
    .WithEnvironment("NLWebNet__EnableStreaming", "true")
    .WithReference(qdrant)  // Connect to Qdrant for vector storage
    .WithReplicas(1); // Single replica for demo purposes

// Add the frontend web application
var frontend = builder.AddProject<Projects.NLWebNet_Frontend>("nlwebnet-frontend")
    .WithReference(nlwebapp)  // Connect to the API
    .WithReplicas(1);

var app = builder.Build();

await app.RunAsync();