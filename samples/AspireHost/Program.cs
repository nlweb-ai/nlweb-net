using NLWebNet.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Add external dependencies (optional - could be databases, message queues, etc.)
// var postgres = builder.AddPostgres("postgres")
//     .WithEnvironment("POSTGRES_DB", "nlwebnet")
//     .PublishAsAzurePostgresFlexibleServer();

// var redis = builder.AddRedis("redis")
//     .PublishAsAzureRedis();

// Add the NLWebNet demo application
var nlwebapp = builder.AddNLWebNetApp("nlwebnet-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("NLWebNet__RateLimiting__RequestsPerWindow", "1000")
    .WithEnvironment("NLWebNet__RateLimiting__WindowSizeInMinutes", "1")
    .WithEnvironment("NLWebNet__EnableStreaming", "true")
    .WithReplicas(2); // Scale out for load testing

// Optional: Add with database dependency
// var nlwebapp = builder.AddNLWebNetAppWithDataBackend("nlwebnet-api", postgres);

// Add a simple frontend (if we had one)
// var frontend = builder.AddProject<Projects.NLWebNet_Frontend>("frontend")
//     .WithReference(nlwebapp);

var app = builder.Build();

await app.RunAsync();