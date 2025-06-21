using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using NLWebNet.Models;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for .NET Aspire integration with NLWebNet
/// </summary>
public static class AspireExtensions
{
    /// <summary>
    /// Adds NLWebNet services configured for .NET Aspire environments
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration callback for NLWebNet options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNetForAspire(
        this IServiceCollection services,
        Action<NLWebOptions>? configureOptions = null)
    {
        // Add standard NLWebNet services
        services.AddNLWebNet(configureOptions);

        // Add service discovery for Aspire
        services.AddServiceDiscovery();

        // Configure OpenTelemetry for Aspire integration
        services.AddNLWebNetOpenTelemetry(builder => builder.ConfigureForAspire());

        // Add health checks optimized for Aspire
        services.AddHealthChecks()
            .AddCheck("aspire-ready", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Ready for Aspire"));

        return services;
    }

    /// <summary>
    /// Adds default service configuration suitable for Aspire-hosted applications
    /// </summary>
    /// <param name="builder">The host application builder</param>
    /// <param name="configureOptions">Optional configuration callback for NLWebNet options</param>
    /// <returns>The host application builder for chaining</returns>
    public static IHostApplicationBuilder AddNLWebNetDefaults(
        this IHostApplicationBuilder builder,
        Action<NLWebOptions>? configureOptions = null)
    {
        // Add NLWebNet services configured for Aspire
        builder.Services.AddNLWebNetForAspire(configureOptions);

        // Configure logging for structured output
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.UseUtcTimestamp = true;
        });

        return builder;
    }
}