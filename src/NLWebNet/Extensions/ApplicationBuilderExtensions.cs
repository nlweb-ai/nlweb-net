using Microsoft.AspNetCore.Builder;

namespace NLWebNet;

/// <summary>
/// Extension methods for configuring NLWebNet endpoints
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Maps NLWebNet endpoints (/ask and /mcp)
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder MapNLWebNet(this IApplicationBuilder app)
    {
        // TODO: Map NLWebNet endpoints here
        // This will be implemented in Phase 5

        return app;
    }
}
