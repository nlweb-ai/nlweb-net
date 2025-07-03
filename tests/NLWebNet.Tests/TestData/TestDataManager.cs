using NLWebNet.Models;
using System.Text.Json;

namespace NLWebNet.Tests.TestData;

/// <summary>
/// Manages test data scenarios for comprehensive testing framework
/// </summary>
public static class TestDataManager
{
    /// <summary>
    /// Gets predefined test scenarios for different query types
    /// </summary>
    public static IEnumerable<TestScenario> GetTestScenarios()
    {
        yield return new TestScenario
        {
            Name = "Basic Search Query",
            Description = "Simple search query testing basic functionality",
            Query = "millennium falcon",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Search },
            MinExpectedResults = 1,
            TestCategories = new[] { TestConstants.Categories.BasicSearch, TestConstants.Categories.EndToEnd }
        };

        yield return new TestScenario
        {
            Name = "Technical Information Query",
            Description = "Query for technical documentation or API information",
            Query = "API documentation for web services",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Search, TestConstants.Tools.Details },
            MinExpectedResults = 1,
            TestCategories = new[] { TestConstants.Categories.Technical, TestConstants.Categories.EndToEnd }
        };

        yield return new TestScenario
        {
            Name = "Compare Query",
            Description = "Comparative query that should trigger compare tool",
            Query = "compare .NET Core vs .NET Framework",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Compare, TestConstants.Tools.Search },
            MinExpectedResults = 1,
            TestCategories = new[] { TestConstants.Categories.Compare, TestConstants.Categories.ToolSelection }
        };

        yield return new TestScenario
        {
            Name = "Details Query",
            Description = "Specific detail query for detailed information",
            Query = "detailed specifications for Enterprise NX-01",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Details, TestConstants.Tools.Search },
            MinExpectedResults = 1,
            TestCategories = new[] { TestConstants.Categories.Details, TestConstants.Categories.ToolSelection }
        };

        yield return new TestScenario
        {
            Name = "Ensemble Query",
            Description = "Complex query requiring ensemble of tools",
            Query = "comprehensive analysis of space exploration technologies",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Ensemble, TestConstants.Tools.Search, TestConstants.Tools.Compare },
            MinExpectedResults = 2,
            TestCategories = new[] { TestConstants.Categories.Ensemble, TestConstants.Categories.Complex }
        };

        yield return new TestScenario
        {
            Name = "Empty Query",
            Description = "Edge case with empty query",
            Query = "",
            ExpectedMode = QueryMode.List,
            ExpectedTools = Array.Empty<string>(),
            MinExpectedResults = 0,
            TestCategories = new[] { TestConstants.Categories.EdgeCase, TestConstants.Categories.Validation }
        };

        yield return new TestScenario
        {
            Name = "Site-Specific Query",
            Description = "Query with site filtering",
            Query = "Dune movie",
            Site = "scifi-cinema.com",
            ExpectedMode = QueryMode.List,
            ExpectedTools = new[] { TestConstants.Tools.Search },
            MinExpectedResults = 1,
            TestCategories = new[] { TestConstants.Categories.SiteFiltering, TestConstants.Categories.EndToEnd }
        };
    }

    /// <summary>
    /// Gets performance benchmark scenarios
    /// </summary>
    public static IEnumerable<PerformanceScenario> GetPerformanceScenarios()
    {
        yield return new PerformanceScenario
        {
            Name = "Single Backend Performance",
            Description = "Baseline performance with single backend",
            Query = "space exploration",
            ExpectedMaxResponseTimeMs = 1000,
            MinIterations = 100,
            BackendCount = 1,
            Category = "Baseline"
        };

        yield return new PerformanceScenario
        {
            Name = "Multi-Backend Performance",
            Description = "Performance with multiple backends enabled",
            Query = "space exploration",
            ExpectedMaxResponseTimeMs = 2000,
            MinIterations = 100,
            BackendCount = 2,
            Category = "MultiBackend"
        };

        yield return new PerformanceScenario
        {
            Name = "Tool Selection Performance",
            Description = "Performance impact of tool selection overhead",
            Query = "compare performance of different web frameworks",
            ExpectedMaxResponseTimeMs = 1500,
            MinIterations = 50,
            BackendCount = 1,
            Category = "ToolSelection"
        };
    }

    /// <summary>
    /// Gets multi-backend consistency test scenarios
    /// </summary>
    public static IEnumerable<ConsistencyScenario> GetConsistencyScenarios()
    {
        yield return new ConsistencyScenario
        {
            Name = "Basic Search Consistency",
            Description = "Verify consistent results across backends for basic search",
            Query = "millennium falcon",
            TolerancePercent = 10,
            MinOverlapPercent = 70,
            Category = "BasicConsistency"
        };

        yield return new ConsistencyScenario
        {
            Name = "Technical Query Consistency",
            Description = "Verify consistency for technical queries",
            Query = "NET Core features",
            TolerancePercent = 15,
            MinOverlapPercent = 60,
            Category = "TechnicalConsistency"
        };
    }
}

/// <summary>
/// Represents a test scenario for comprehensive testing
/// </summary>
public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string? Site { get; set; }
    public QueryMode ExpectedMode { get; set; } = QueryMode.List;
    public string[] ExpectedTools { get; set; } = Array.Empty<string>();
    public int MinExpectedResults { get; set; }
    public string[] TestCategories { get; set; } = Array.Empty<string>();

    public NLWebRequest ToRequest(string? queryId = null)
    {
        return new NLWebRequest
        {
            QueryId = queryId ?? $"test-{Guid.NewGuid():N}",
            Query = Query,
            Site = Site,
            Mode = ExpectedMode
        };
    }
}

/// <summary>
/// Represents a performance benchmark scenario
/// </summary>
public class PerformanceScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int ExpectedMaxResponseTimeMs { get; set; }
    public int MinIterations { get; set; }
    public int BackendCount { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a multi-backend consistency test scenario
/// </summary>
public class ConsistencyScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public double TolerancePercent { get; set; }
    public double MinOverlapPercent { get; set; }
    public string Category { get; set; } = string.Empty;
}