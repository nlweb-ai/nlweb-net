using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NLWebNet.Endpoints;

/// <summary>
/// Minimal API endpoints for health checks and monitoring
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health check endpoints to the application
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Basic health check endpoint
        app.MapGet("/health", GetBasicHealthAsync)
            .WithName("GetHealth")
            .WithTags("Health")
            .WithSummary("Basic health check")
            .WithDescription("Returns the basic health status of the NLWebNet service")
            .Produces<HealthCheckResponse>(StatusCodes.Status200OK)
            .Produces<HealthCheckResponse>(StatusCodes.Status503ServiceUnavailable);

        // Detailed health check endpoint
        app.MapGet("/health/detailed", GetDetailedHealthAsync)
            .WithName("GetDetailedHealth")
            .WithTags("Health")
            .WithSummary("Detailed health check")
            .WithDescription("Returns detailed health status including individual service checks")
            .Produces<DetailedHealthCheckResponse>(StatusCodes.Status200OK)
            .Produces<DetailedHealthCheckResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetBasicHealthAsync(
        [FromServices] HealthCheckService healthCheckService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger(nameof(HealthEndpoints));

        try
        {
            var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);

            var response = new HealthCheckResponse
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable;

            logger.LogInformation("Health check completed with status: {Status}", healthReport.Status);

            return Results.Json(response, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed with exception");

            var response = new HealthCheckResponse
            {
                Status = "Unhealthy",
                TotalDuration = TimeSpan.Zero
            };

            return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> GetDetailedHealthAsync(
        [FromServices] HealthCheckService healthCheckService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger(nameof(HealthEndpoints));

        try
        {
            var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);

            var response = new DetailedHealthCheckResponse
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Entries = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthCheckEntry
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Exception = kvp.Value.Exception?.Message,
                        Data = kvp.Value.Data.Any() ? kvp.Value.Data : null
                    })
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable;

            logger.LogInformation("Detailed health check completed with status: {Status}, Entries: {EntryCount}",
                healthReport.Status, healthReport.Entries.Count);

            return Results.Json(response, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Detailed health check failed with exception");

            var response = new DetailedHealthCheckResponse
            {
                Status = "Unhealthy",
                TotalDuration = TimeSpan.Zero,
                Entries = new Dictionary<string, HealthCheckEntry>
                {
                    ["system"] = new HealthCheckEntry
                    {
                        Status = "Unhealthy",
                        Description = "Health check system failure",
                        Duration = TimeSpan.Zero,
                        Exception = ex.Message
                    }
                }
            };

            return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}

/// <summary>
/// Basic health check response
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
}

/// <summary>
/// Detailed health check response with individual service status
/// </summary>
public class DetailedHealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, HealthCheckEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual health check entry details
/// </summary>
public class HealthCheckEntry
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Exception { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}