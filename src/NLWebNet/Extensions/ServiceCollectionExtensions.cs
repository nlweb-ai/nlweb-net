using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.MCP;
using NLWebNet.Controllers;
using NLWebNet.Health;

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

        // Register controllers
        services.AddTransient<AskController>();
        services.AddTransient<McpController>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<NLWebHealthCheck>("nlweb")
            .AddCheck<DataBackendHealthCheck>("data-backend")
            .AddCheck<AIServiceHealthCheck>("ai-service");

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

        return services;
    }
}
