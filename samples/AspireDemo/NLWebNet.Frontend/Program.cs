using NLWebNet.Demo.Components;
using NLWebNet.Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

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

// Register the default HttpClient for component injection
builder.Services.AddScoped(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("ApiClient");
});

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
