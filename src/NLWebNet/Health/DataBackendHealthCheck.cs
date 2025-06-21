using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NLWebNet.Services;

namespace NLWebNet.Health;

/// <summary>
/// Health check for data backend connectivity
/// </summary>
public class DataBackendHealthCheck : IHealthCheck
{
    private readonly IDataBackend _dataBackend;
    private readonly ILogger<DataBackendHealthCheck> _logger;

    public DataBackendHealthCheck(IDataBackend dataBackend, ILogger<DataBackendHealthCheck> logger)
    {
        _dataBackend = dataBackend ?? throw new ArgumentNullException(nameof(dataBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing data backend health check");

            // Check if the data backend is responsive
            if (_dataBackend == null)
            {
                return HealthCheckResult.Unhealthy("Data backend is not available");
            }

            // Test basic connectivity by attempting a simple query
            // This is a lightweight check that doesn't impact performance
            var testResults = await _dataBackend.SearchAsync("health-check", cancellationToken: cancellationToken);

            // The search should complete without throwing an exception
            // We don't care about the results, just that the backend is responsive

            _logger.LogDebug("Data backend health check completed successfully");
            return HealthCheckResult.Healthy($"Data backend ({_dataBackend.GetType().Name}) is operational");
        }
        catch (NotImplementedException)
        {
            // Some backends might not implement SearchAsync
            _logger.LogDebug("Data backend doesn't support SearchAsync, checking availability only");
            return HealthCheckResult.Healthy($"Data backend ({_dataBackend.GetType().Name}) is available (limited functionality)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data backend health check failed");
            return HealthCheckResult.Unhealthy($"Data backend health check failed: {ex.Message}", ex);
        }
    }
}