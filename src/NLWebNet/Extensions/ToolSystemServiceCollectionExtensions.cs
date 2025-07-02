using Microsoft.Extensions.DependencyInjection;
using NLWebNet.Services;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for registering the Advanced Tool System services.
/// </summary>
public static class ToolSystemServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Advanced Tool System services to the dependency injection container.
    /// This includes all tool handlers and the tool executor.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAdvancedToolSystem(this IServiceCollection services)
    {
        // Register the tool executor
        services.AddScoped<IToolExecutor, ToolExecutor>();

        // Register all tool handlers
        services.AddScoped<IToolHandler, SearchToolHandler>();
        services.AddScoped<IToolHandler, DetailsToolHandler>();
        services.AddScoped<IToolHandler, CompareToolHandler>();
        services.AddScoped<IToolHandler, EnsembleToolHandler>();
        services.AddScoped<IToolHandler, RecipeToolHandler>();

        // Register tool definition loader (already exists but ensure it's registered)
        services.AddScoped<IToolDefinitionLoader, ToolDefinitionLoader>();

        return services;
    }
}