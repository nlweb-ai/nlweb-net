using Microsoft.Extensions.DependencyInjection;

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

        // TODO: Register NLWebNet services here
        // services.AddScoped<INLWebService, NLWebService>();
        // services.AddScoped<IQueryProcessor, QueryProcessor>();
        // services.AddScoped<IResultGenerator, ResultGenerator>();

        return services;
    }
}
