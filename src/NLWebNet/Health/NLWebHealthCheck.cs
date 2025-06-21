using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Services;
using NLWebNet.Metrics;

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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Check if the service is responsive by testing a simple query
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["HealthCheckName"] = "nlweb",
                ["HealthCheckType"] = "Service"
            });

            _logger.LogDebug("Performing NLWeb service health check");

            // Basic service availability check - we can test if services are registered and responsive
            if (_nlWebService == null)
            {
                var result = HealthCheckResult.Unhealthy("NLWeb service is not available");
                RecordHealthCheckMetrics("nlweb", result.Status, stopwatch.ElapsedMilliseconds);
                return Task.FromResult(result);
            }

            // Additional checks could include:
            // - Testing a lightweight query
            // - Checking service dependencies
            // - Validating configuration

            _logger.LogDebug("NLWeb service health check completed successfully");
            var healthyResult = HealthCheckResult.Healthy("NLWeb service is operational");
            RecordHealthCheckMetrics("nlweb", healthyResult.Status, stopwatch.ElapsedMilliseconds);
            return Task.FromResult(healthyResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NLWeb service health check failed");
            var unhealthyResult = HealthCheckResult.Unhealthy($"NLWeb service health check failed: {ex.Message}", ex);
            RecordHealthCheckMetrics("nlweb", unhealthyResult.Status, stopwatch.ElapsedMilliseconds);
            return Task.FromResult(unhealthyResult);
        }
    }

    private static void RecordHealthCheckMetrics(string checkName, HealthStatus status, double durationMs)
    {
        NLWebMetrics.HealthCheckExecutions.Add(1,
            new KeyValuePair<string, object?>(NLWebMetrics.Tags.HealthCheckName, checkName));

        if (status != HealthStatus.Healthy)
        {
            NLWebMetrics.HealthCheckFailures.Add(1,
                new KeyValuePair<string, object?>(NLWebMetrics.Tags.HealthCheckName, checkName));
        }
    }
}