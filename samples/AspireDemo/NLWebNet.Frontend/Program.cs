using NLWebNet.Demo.Components;
using NLWebNet.Frontend.Components;
using NLWebNet.Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// Configure additional OpenTelemetry sources for our custom activities
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("NLWebNet.Frontend.ApiService");
        tracing.AddSource("NLWebNet.Frontend.VectorSearch");
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP client for API calls with service discovery
builder.Services.AddHttpClient("ApiClient", client =>
{
    // Use service discovery to find the API service - try HTTPS first
    client.BaseAddress = new Uri("https://nlwebnet-aspire-api");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add a backup HttpClient with direct URL for debugging
builder.Services.AddHttpClient("DirectApiClient", client =>
{
    // Use the actual API URL from Aspire dashboard
    client.BaseAddress = new Uri("https://localhost:7220");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add dedicated HttpClient for RSS operations with longer timeout
builder.Services.AddHttpClient("RssApiClient", client =>
{
    // Use the actual API URL from Aspire dashboard
    client.BaseAddress = new Uri("https://localhost:7220");
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minutes for RSS ingestion
});

// Register the default HttpClient for component injection
builder.Services.AddScoped(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    // Temporarily use DirectApiClient for debugging
    return factory.CreateClient("DirectApiClient");
});

// Register configuration service
builder.Services.AddScoped<IEmbeddingConfigurationService, EmbeddingConfigurationService>();

// Register API service
builder.Services.AddScoped<IApiService, ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.MapDefaultEndpoints();

app.Run();
