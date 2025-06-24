using Microsoft.AspNetCore.Builder;
using NLWebNet;
using NLWebNet.Extensions;
using NLWebNet.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Detect if running in Aspire and configure accordingly
var isAspireEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ASPIRE_URLS")) ||
                     !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for Blazor components
builder.Services.AddHttpClient();

// Add AI configuration service as singleton to persist configuration across requests
builder.Services.AddSingleton<NLWebNet.Demo.Services.IAIConfigurationService, NLWebNet.Demo.Services.AIConfigurationService>();

// Add dynamic chat client factory
builder.Services.AddScoped<NLWebNet.Demo.Services.IDynamicChatClientFactory, NLWebNet.Demo.Services.DynamicChatClientFactory>();

// Register a factory-based IChatClient for NLWebNet that resolves dynamically
builder.Services.AddTransient<Microsoft.Extensions.AI.IChatClient>(serviceProvider =>
{
    var factory = serviceProvider.GetRequiredService<NLWebNet.Demo.Services.IDynamicChatClientFactory>();
    return factory.GetChatClient() ?? new NLWebNet.Demo.Services.NullChatClient();
});

// Explicitly use MockDataBackend to test if WebSearchBackend is causing the hang
builder.Services.AddScoped<NLWebNet.Services.IDataBackend>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<NLWebNet.Services.MockDataBackend>>();
    return new NLWebNet.Services.MockDataBackend(logger);
});

// Temporarily use MockDataBackend to test if WebSearchBackend is causing the hang
// builder.Services.AddScoped<NLWebNet.Services.IDataBackend>(serviceProvider =>
// {
//     var logger = serviceProvider.GetRequiredService<ILogger<NLWebNet.Demo.Services.WebSearchBackend>>();
//     var httpClient = serviceProvider.GetRequiredService<HttpClient>();
//     return new NLWebNet.Demo.Services.WebSearchBackend(logger, httpClient);
// });

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        var corsSettings = builder.Configuration.GetSection("CORS");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
        var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "OPTIONS" };
        var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "Content-Type", "Authorization" };
        var allowCredentials = corsSettings.GetValue<bool>("AllowCredentials");

        corsBuilder.WithOrigins(allowedOrigins)
                   .WithMethods(allowedMethods)
                   .WithHeaders(allowedHeaders);

        if (allowCredentials)
            corsBuilder.AllowCredentials();
    });
});

// Add NLWebNet services - use Aspire-optimized version if available
if (isAspireEnabled)
{
    builder.Services.AddNLWebNetForAspire(options =>
    {
        // Configure NLWebNet options here
        options.DefaultMode = NLWebNet.Models.QueryMode.List;
        options.EnableStreaming = true;
        // Aspire environments typically handle more load
        options.RateLimiting.RequestsPerWindow = 1000;
        options.RateLimiting.WindowSizeInMinutes = 1;
    });
}
else
{
    builder.Services.AddNLWebNet(options =>
    {
        // Configure NLWebNet options here
        options.DefaultMode = NLWebNet.Models.QueryMode.List;
        options.EnableStreaming = true;
    });    // Add OpenTelemetry for non-Aspire environments (development/testing)
    // Disable console exporters to reduce terminal noise
    builder.Services.AddNLWebNetOpenTelemetry("NLWebNet.Demo", "1.0.0", otlBuilder =>
    {
        // Comment out console exporters for cleaner development experience
        // otlBuilder.AddConsoleExporters(); // Simple console output for development
    });
}

// Add OpenAPI for API documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Add NLWebNet middleware
app.UseNLWebNet();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<NLWebNet.Demo.Components.App>()
    .AddInteractiveServerRenderMode();

// Map NLWebNet minimal API endpoints
Console.WriteLine($"[DEBUG] Mapping NLWebNet endpoints at {DateTime.Now}");
app.MapNLWebNet();
Console.WriteLine($"[DEBUG] NLWebNet endpoints mapped at {DateTime.Now}");

app.Run();
