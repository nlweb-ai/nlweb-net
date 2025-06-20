using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the NLWebNet Demo web application
var nlwebnetDemo = builder.AddProject("nlwebnet-demo", "../demo/NLWebNet.Demo.csproj")
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("NLWebNet__DefaultMode", "List")
    .WithEnvironment("NLWebNet__EnableStreaming", "true")
    .WithEnvironment("NLWebNet__DefaultTimeoutSeconds", "30")
    .WithEnvironment("NLWebNet__MaxResultsPerQuery", "50");

// Build and run the application
var app = builder.Build();

await app.RunAsync();