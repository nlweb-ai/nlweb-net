using Microsoft.AspNetCore.Builder;
using NLWebNet;
using NLWebNet.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// Add NLWebNet services
builder.Services.AddNLWebNet(options =>
{
    // Configure NLWebNet options here
    options.DefaultMode = NLWebNet.Models.QueryMode.List;
    options.EnableStreaming = true;
});

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
app.MapNLWebNet();

// Add health check endpoint for container health monitoring
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.Run();
