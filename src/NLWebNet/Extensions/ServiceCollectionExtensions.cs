using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.MCP;
using NLWebNet.Health;
using NLWebNet.RateLimiting;
using NLWebNet.Metrics;
using System.Diagnostics;

namespace NLWebNet;

/// <summary>
/// Extension methods for configuring NLWebNet services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NLWebNet services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration callback</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNet(this IServiceCollection services, Action<NLWebOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register core NLWebNet services
        services.AddScoped<INLWebService, NLWebService>();
        services.AddScoped<IQueryProcessor, QueryProcessor>();
        services.AddScoped<IResultGenerator, ResultGenerator>();

        // Register MCP services
        services.AddScoped<IMcpService, McpService>();        // Register default data backend (can be overridden)
        services.AddScoped<IDataBackend, MockDataBackend>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<NLWebHealthCheck>("nlweb")
            .AddCheck<DataBackendHealthCheck>("data-backend")
            .AddCheck<AIServiceHealthCheck>("ai-service");

        // Add metrics
        services.AddMetrics();

        // Add rate limiting
        services.AddSingleton<IRateLimitingService, InMemoryRateLimitingService>();

        return services;
    }

    /// <summary>
    /// Adds NLWebNet services with a custom data backend
    /// </summary>
    /// <typeparam name="TDataBackend">The custom data backend implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration callback</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNet<TDataBackend>(this IServiceCollection services, Action<NLWebOptions>? configureOptions = null)
        where TDataBackend : class, IDataBackend
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register core NLWebNet services
        services.AddScoped<INLWebService, NLWebService>();
        services.AddScoped<IQueryProcessor, QueryProcessor>();
        services.AddScoped<IResultGenerator, ResultGenerator>();

        // Register MCP services
        services.AddScoped<IMcpService, McpService>();

        // Register custom data backend
        services.AddScoped<IDataBackend, TDataBackend>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<NLWebHealthCheck>("nlweb")
            .AddCheck<DataBackendHealthCheck>("data-backend")
            .AddCheck<AIServiceHealthCheck>("ai-service");

        // Add metrics
        services.AddMetrics();

        // Add rate limiting
        services.AddSingleton<IRateLimitingService, InMemoryRateLimitingService>();

        return services;
    }

    /// <summary>
    /// Adds NLWebNet services with multi-backend support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration callback</param>
    /// <param name="configureMultiBackend">Multi-backend configuration callback</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNLWebNetMultiBackend(this IServiceCollection services,
        Action<NLWebOptions>? configureOptions = null,
        Action<MultiBackendOptions>? configureMultiBackend = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Configure multi-backend options
        if (configureMultiBackend != null)
        {
            services.Configure<MultiBackendOptions>(configureMultiBackend);
        }

        // Add logging (required for the services)
        services.AddLogging();

        // Register core NLWebNet services
        services.AddScoped<INLWebService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<NLWebOptions>>();
            var multiBackendOptions = provider.GetRequiredService<IOptions<MultiBackendOptions>>();
            if (multiBackendOptions.Value.Enabled)
            {
                // Use multi-backend constructor
                return new NLWebService(
                    provider.GetRequiredService<IQueryProcessor>(),
                    provider.GetRequiredService<IResultGenerator>(),
                    provider.GetRequiredService<IBackendManager>(),
                    provider.GetRequiredService<ILogger<NLWebService>>(),
                    options);
            }
            else
            {
                // Use single backend constructor for backward compatibility
                return new NLWebService(
                    provider.GetRequiredService<IQueryProcessor>(),
                    provider.GetRequiredService<IResultGenerator>(),
                    provider.GetRequiredService<IDataBackend>(),
                    provider.GetRequiredService<ILogger<NLWebService>>(),
                    options);
            }
        });

        services.AddScoped<IQueryProcessor, QueryProcessor>();
        services.AddScoped<IResultGenerator>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<NLWebOptions>>();
            var multiBackendOptions = provider.GetRequiredService<IOptions<MultiBackendOptions>>();
            if (multiBackendOptions.Value.Enabled)
            {
                // Use multi-backend constructor
                return new ResultGenerator(
                    provider.GetRequiredService<IBackendManager>(),
                    provider.GetRequiredService<ILogger<ResultGenerator>>(),
                    options,
                    provider.GetService<IChatClient>());
            }
            else
            {
                // Use single backend constructor for backward compatibility
                return new ResultGenerator(
                    provider.GetRequiredService<IDataBackend>(),
                    provider.GetRequiredService<ILogger<ResultGenerator>>(),
                    options,
                    provider.GetService<IChatClient>());
            }
        });

        // Register MCP services
        services.AddScoped<IMcpService, McpService>();

        // Register multi-backend services
        services.AddScoped<IBackendManager, BackendManager>();

        // Register default data backend (can be overridden)
        services.AddScoped<IDataBackend, MockDataBackend>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<NLWebHealthCheck>("nlweb")
            .AddCheck<DataBackendHealthCheck>("data-backend")
            .AddCheck<AIServiceHealthCheck>("ai-service");

        // Add metrics
        services.AddMetrics();

        // Add rate limiting
        services.AddSingleton<IRateLimitingService, InMemoryRateLimitingService>();

        return services;
    }
}
