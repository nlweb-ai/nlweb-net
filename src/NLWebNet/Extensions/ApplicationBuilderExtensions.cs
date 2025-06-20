using Microsoft.AspNetCore.Builder;
using NLWebNet.Middleware;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for configuring NLWebNet middleware and endpoints
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds NLWebNet middleware to the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseNLWebNet(this IApplicationBuilder app)
    {
        app.UseMiddleware<NLWebMiddleware>();
        return app;
    }

    /// <summary>
    /// Maps NLWebNet API endpoints (/ask and /mcp)
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder MapNLWebNet(this IApplicationBuilder app)
    {
        // Controllers will be automatically mapped via [Route] attributes when using AddControllers()
        // This method is available for future route configuration if needed
        return app;
    }
}
