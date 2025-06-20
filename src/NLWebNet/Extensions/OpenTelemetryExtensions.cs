using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using NLWebNet.Metrics;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry with NLWebNet
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry integration to NLWebNet with sensible defaults
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">The service name for telemetry</param>
    /// <param name="serviceVersion">The service version for telemetry</param>
    /// <param name="configure">Optional configuration callback for additional OpenTelemetry setup</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNetOpenTelemetry(
        this IServiceCollection services,
        string serviceName = "NLWebNet",
        string serviceVersion = "1.0.0",
        Action<OpenTelemetryBuilder>? configure = null)
    {
        return services.AddNLWebNetOpenTelemetry(builder =>
        {
            builder.ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("service.namespace", "nlwebnet"),
                    new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
                }));
            
            // Apply additional configuration if provided
            configure?.Invoke(builder);
        });
    }

    /// <summary>
    /// Adds OpenTelemetry integration to NLWebNet with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration callback for OpenTelemetry</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNetOpenTelemetry(
        this IServiceCollection services,
        Action<OpenTelemetryBuilder> configure)
    {
        var builder = services.AddOpenTelemetry();

        // Configure default resource
        builder.ConfigureResource(resource => resource
            .AddService("NLWebNet", "1.0.0")
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk());

        // Configure metrics
        builder.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(NLWebMetrics.MeterName)
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));
            // .AddRuntimeInstrumentation() - This requires additional packages

        // Configure tracing
        builder.WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context =>
                {
                    // Filter out health check requests from tracing
                    var path = context.Request.Path.Value;
                    return !string.IsNullOrEmpty(path) && !path.StartsWith("/health");
                };
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("nlweb.request.path", request.Path);
                    activity.SetTag("nlweb.request.method", request.Method);
                    if (request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
                    {
                        activity.SetTag("nlweb.correlation_id", correlationId.FirstOrDefault());
                    }
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("nlweb.response.status_code", response.StatusCode);
                };
            })
            .AddHttpClientInstrumentation()
            .AddSource(NLWebMetrics.MeterName)
            .SetSampler(new TraceIdRatioBasedSampler(0.1))); // Sample 10% of traces by default

        // Configure logging
        builder.WithLogging(logging => logging
            .AddConsoleExporter());

        // Apply custom configuration
        configure(builder);

        return services;
    }

    /// <summary>
    /// Adds console exporter for OpenTelemetry (useful for development)
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <returns>The OpenTelemetry builder for chaining</returns>
    public static OpenTelemetryBuilder AddConsoleExporters(this OpenTelemetryBuilder builder)
    {
        return builder
            .WithMetrics(metrics => metrics.AddConsoleExporter())
            .WithTracing(tracing => tracing.AddConsoleExporter());
    }

    /// <summary>
    /// Adds OTLP (OpenTelemetry Protocol) exporter for sending telemetry to collectors
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <param name="endpoint">The OTLP endpoint URL</param>
    /// <returns>The OpenTelemetry builder for chaining</returns>
    public static OpenTelemetryBuilder AddOtlpExporters(this OpenTelemetryBuilder builder, string? endpoint = null)
    {
        return builder
            .WithMetrics(metrics => metrics.AddOtlpExporter(options =>
            {
                if (!string.IsNullOrEmpty(endpoint))
                    options.Endpoint = new Uri(endpoint);
            }))
            .WithTracing(tracing => tracing.AddOtlpExporter(options =>
            {
                if (!string.IsNullOrEmpty(endpoint))
                    options.Endpoint = new Uri(endpoint);
            }));
    }

    /// <summary>
    /// Adds Prometheus metrics exporter with HTTP endpoint
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <returns>The OpenTelemetry builder for chaining</returns>
    public static OpenTelemetryBuilder AddPrometheusExporter(this OpenTelemetryBuilder builder)
    {
        return builder.WithMetrics(metrics => metrics.AddPrometheusExporter());
    }

    /// <summary>
    /// Configures OpenTelemetry for .NET Aspire integration
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <returns>The OpenTelemetry builder for chaining</returns>
    public static OpenTelemetryBuilder ConfigureForAspire(this OpenTelemetryBuilder builder)
    {
        // Aspire automatically configures OTLP exporters via environment variables
        // This method ensures optimal settings for Aspire dashboard integration
        return builder
            .WithMetrics(metrics => metrics
                .AddOtlpExporter()) // Aspire configures endpoint via OTEL_EXPORTER_OTLP_ENDPOINT
            .WithTracing(tracing => tracing
                .AddOtlpExporter() // Aspire configures endpoint via OTEL_EXPORTER_OTLP_ENDPOINT
                .SetSampler(new AlwaysOnSampler())); // Aspire dashboard benefits from more traces
    }
}