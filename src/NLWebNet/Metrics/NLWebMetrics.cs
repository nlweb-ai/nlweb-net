using System.Diagnostics.Metrics;

namespace NLWebNet.Metrics;

/// <summary>
/// Contains metric definitions and constants for NLWebNet monitoring
/// </summary>
public static class NLWebMetrics
{
    /// <summary>
    /// The meter name for NLWebNet metrics
    /// </summary>
    public const string MeterName = "NLWebNet";

    /// <summary>
    /// The version for metrics tracking
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// Shared meter instance for all NLWebNet metrics
    /// </summary>
    public static readonly Meter Meter = new(MeterName, Version);

    // Request/Response Metrics
    public static readonly Counter<long> RequestCount = Meter.CreateCounter<long>(
        "nlweb.requests.total",
        description: "Total number of requests processed");

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "nlweb.request.duration",
        unit: "ms",
        description: "Duration of request processing in milliseconds");

    public static readonly Counter<long> RequestErrors = Meter.CreateCounter<long>(
        "nlweb.requests.errors",
        description: "Total number of request errors");

    // AI Service Metrics
    public static readonly Counter<long> AIServiceCalls = Meter.CreateCounter<long>(
        "nlweb.ai.calls.total",
        description: "Total number of AI service calls");

    public static readonly Histogram<double> AIServiceDuration = Meter.CreateHistogram<double>(
        "nlweb.ai.duration",
        unit: "ms",
        description: "Duration of AI service calls in milliseconds");

    public static readonly Counter<long> AIServiceErrors = Meter.CreateCounter<long>(
        "nlweb.ai.errors",
        description: "Total number of AI service errors");

    // Data Backend Metrics
    public static readonly Counter<long> DataBackendQueries = Meter.CreateCounter<long>(
        "nlweb.data.queries.total",
        description: "Total number of data backend queries");

    public static readonly Histogram<double> DataBackendDuration = Meter.CreateHistogram<double>(
        "nlweb.data.duration",
        unit: "ms",
        description: "Duration of data backend operations in milliseconds");

    public static readonly Counter<long> DataBackendErrors = Meter.CreateCounter<long>(
        "nlweb.data.errors",
        description: "Total number of data backend errors");

    // Health Check Metrics
    public static readonly Counter<long> HealthCheckExecutions = Meter.CreateCounter<long>(
        "nlweb.health.checks.total",
        description: "Total number of health check executions");

    public static readonly Counter<long> HealthCheckFailures = Meter.CreateCounter<long>(
        "nlweb.health.failures",
        description: "Total number of health check failures");

    // Business Metrics
    public static readonly Counter<long> QueryTypeCount = Meter.CreateCounter<long>(
        "nlweb.queries.by_type",
        description: "Count of queries by type (List, Summarize, Generate)");

    public static readonly Histogram<double> QueryComplexity = Meter.CreateHistogram<double>(
        "nlweb.queries.complexity",
        description: "Query complexity score based on length and structure");

    /// <summary>
    /// Common tag keys for consistent metric labeling
    /// </summary>
    public static class Tags
    {
        public const string Endpoint = "endpoint";
        public const string Method = "method";
        public const string StatusCode = "status_code";
        public const string QueryMode = "query_mode";
        public const string ErrorType = "error_type";
        public const string HealthCheckName = "health_check";
        public const string DataBackendType = "backend_type";
        public const string AIServiceType = "ai_service_type";
    }
}