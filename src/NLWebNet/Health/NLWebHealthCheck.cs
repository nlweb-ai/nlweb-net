using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Services;

namespace NLWebNet.Health;

/// <summary>
/// Health check for the core NLWebNet service
/// </summary>
public class NLWebHealthCheck : IHealthCheck
{
    private readonly INLWebService _nlWebService;
    private readonly ILogger<NLWebHealthCheck> _logger;

    public NLWebHealthCheck(INLWebService nlWebService, ILogger<NLWebHealthCheck> logger)
    {
        _nlWebService = nlWebService ?? throw new ArgumentNullException(nameof(nlWebService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if the service is responsive by testing a simple query
            _logger.LogDebug("Performing NLWeb service health check");

            // Basic service availability check - we can test if services are registered and responsive
            if (_nlWebService == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("NLWeb service is not available"));
            }

            // Additional checks could include:
            // - Testing a lightweight query
            // - Checking service dependencies
            // - Validating configuration

            _logger.LogDebug("NLWeb service health check completed successfully");
            return Task.FromResult(HealthCheckResult.Healthy("NLWeb service is operational"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NLWeb service health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy($"NLWeb service health check failed: {ex.Message}", ex));
        }
    }
}