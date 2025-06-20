using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.MCP;

namespace NLWebNet.Health;

/// <summary>
/// Health check for AI/MCP service connectivity
/// </summary>
public class AIServiceHealthCheck : IHealthCheck
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<AIServiceHealthCheck> _logger;

    public AIServiceHealthCheck(IMcpService mcpService, ILogger<AIServiceHealthCheck> logger)
    {
        _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing AI service health check");

            // Check if the MCP service is responsive
            if (_mcpService == null)
            {
                return HealthCheckResult.Unhealthy("AI/MCP service is not available");
            }

            // Test basic connectivity by checking available tools
            // This is a lightweight operation that validates the service is operational
            var toolsResult = await _mcpService.ListToolsAsync(cancellationToken);
            
            if (toolsResult == null)
            {
                return HealthCheckResult.Degraded("AI/MCP service responded but returned null tools list");
            }

            _logger.LogDebug("AI service health check completed successfully");
            return HealthCheckResult.Healthy("AI/MCP service is operational");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service health check failed");
            return HealthCheckResult.Unhealthy($"AI service health check failed: {ex.Message}", ex);
        }
    }
}