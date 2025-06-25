using NLWebNet.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Configure logging to reduce telemetry noise while keeping important startup messages
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    // Keep important Aspire startup messages but filter telemetry
    options.AddFilter("Aspire.Hosting.ApplicationModel", LogLevel.Information);
    options.AddFilter("Aspire.Hosting", LogLevel.Information);
    options.AddFilter("Aspire", LogLevel.Warning);
    
    // Reduce OpenTelemetry noise
    options.AddFilter("OpenTelemetry", LogLevel.Warning);
    
    // Keep basic hosting messages
    options.AddFilter("Microsoft.Extensions.Hosting.Internal.Host", LogLevel.Information);
    options.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
    
    // Reduce ASP.NET Core noise but keep startup messages
    options.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Information);
    options.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    
    // Reduce DI and HTTP noise
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