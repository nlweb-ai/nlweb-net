using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NLWebNet.Middleware;
using NLWebNet.Endpoints;

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
    }    /// <summary>
         /// Maps NLWebNet API endpoints (/ask and /mcp) using minimal APIs
         /// </summary>
         /// <param name="app">The web application</param>
         /// <returns>The web application for chaining</returns>
    public static WebApplication MapNLWebNet(this WebApplication app)
    {
        // Map minimal API endpoints directly
        AskEndpoints.MapAskEndpoints(app);
        McpEndpoints.MapMcpEndpoints(app);

        return app;
    }

    /// <summary>
    /// Maps NLWebNet API endpoints (/ask and /mcp) using minimal APIs
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapNLWebNet(this IEndpointRouteBuilder app)
    {
        // Map minimal API endpoints directly
        AskEndpoints.MapAskEndpoints(app);
        McpEndpoints.MapMcpEndpoints(app);

        return app;
    }

    /// <summary>
    /// Maps NLWebNet API controllers (/ask and /mcp) - Legacy controller support
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder MapNLWebNetControllers(this IApplicationBuilder app)
    {
        // Controllers will be automatically mapped via [Route] attributes when using AddControllers()
        // This method is available for future route configuration if needed
        return app;
    }
}
