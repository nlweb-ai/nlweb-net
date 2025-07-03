using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;
using NLWebNet.Tests.TestData;
using System.Diagnostics;

namespace NLWebNet.Tests.Performance;

/// <summary>
/// Performance benchmarking framework for automated regression testing
/// </summary>
[TestClass]
public class PerformanceBenchmarkFramework
{
    /// <summary>
    /// Runs comprehensive performance benchmarks for all scenarios
    /// </summary>
    [TestMethod]
    public async Task Performance_ComprehensiveBenchmarks_MeetExpectedThresholds()
    {
        var performanceScenarios = TestDataManager.GetPerformanceScenarios();
        var results = new List<BenchmarkResult>();

        foreach (var scenario in performanceScenarios)
        {
            Console.WriteLine($"Running performance benchmark: {scenario.Name}");

            var benchmarkResult = await RunPerformanceBenchmark(scenario);
            results.Add(benchmarkResult);

            // Assert performance meets expectations
            Assert.IsTrue(benchmarkResult.AverageResponseTimeMs <= scenario.ExpectedMaxResponseTimeMs,
                $"Average response time ({benchmarkResult.AverageResponseTimeMs:F2}ms) should be <= " +
                $"{scenario.ExpectedMaxResponseTimeMs}ms for scenario: {scenario.Name}");

            Console.WriteLine($"✓ Benchmark '{scenario.Name}' passed");
            Console.WriteLine($"  Average: {benchmarkResult.AverageResponseTimeMs:F2}ms");
            Console.WriteLine($"  Min: {benchmarkResult.MinResponseTimeMs:F2}ms");
            Console.WriteLine($"  Max: {benchmarkResult.MaxResponseTimeMs:F2}ms");
            Console.WriteLine($"  95th percentile: {benchmarkResult.Percentile95Ms:F2}ms");
        }

        // Generate performance report
        GeneratePerformanceReport(results);
    }

    /// <summary>
    /// Tests performance regression by comparing with baseline metrics
    /// </summary>
    [TestMethod]
    public async Task Performance_RegressionTesting_NoSignificantDegradation()
    {
        var baselineScenario = TestDataManager.GetPerformanceScenarios()
            .First(s => s.Category == "Baseline");

        Console.WriteLine($"Running regression test for: {baselineScenario.Name}");

        var benchmarkResult = await RunPerformanceBenchmark(baselineScenario);

        // For regression testing, we would typically compare against stored baseline metrics
        // For now, we'll use the expected max as our baseline
        var baselineMs = baselineScenario.ExpectedMaxResponseTimeMs;
        var regressionThresholdPercent = 50; // Allow 50% degradation as threshold for test environment
        var maxAllowedMs = baselineMs * (1 + regressionThresholdPercent / 100.0);

        Assert.IsTrue(benchmarkResult.AverageResponseTimeMs <= maxAllowedMs,
            $"Performance regression detected. Average response time ({benchmarkResult.AverageResponseTimeMs:F2}ms) " +
            $"exceeds regression threshold ({maxAllowedMs:F2}ms) based on baseline ({baselineMs}ms)");

        Console.WriteLine($"✓ No significant performance regression detected");
        Console.WriteLine($"Baseline threshold: {baselineMs}ms");
        Console.WriteLine($"Regression threshold: {maxAllowedMs:F2}ms");
        Console.WriteLine($"Actual average: {benchmarkResult.AverageResponseTimeMs:F2}ms");
    }

    /// <summary>
    /// Tests multi-backend performance impact
    /// </summary>
    [TestMethod]
    public async Task Performance_MultiBackendImpact_WithinAcceptableLimits()
    {
        var singleBackendResult = await BenchmarkConfiguration("Single Backend", false);
        var multiBackendResult = await BenchmarkConfiguration("Multi Backend", true);

        var performanceImpactPercent =
            ((multiBackendResult.AverageResponseTimeMs - singleBackendResult.AverageResponseTimeMs) /
             singleBackendResult.AverageResponseTimeMs) * 100;

        Console.WriteLine($"Single backend average: {singleBackendResult.AverageResponseTimeMs:F2}ms");
        Console.WriteLine($"Multi backend average: {multiBackendResult.AverageResponseTimeMs:F2}ms");
        Console.WriteLine($"Performance impact: {performanceImpactPercent:F2}%");

        // Multi-backend should not cause more than 100% performance degradation in test environment
        Assert.IsTrue(performanceImpactPercent <= 100,
            $"Multi-backend performance impact ({performanceImpactPercent:F2}%) should be within acceptable limits");

        Console.WriteLine("✓ Multi-backend performance impact within acceptable limits");
    }

    /// <summary>
    /// Tests performance under load with concurrent requests
    /// </summary>
    [TestMethod]
    public async Task Performance_ConcurrentLoad_HandlesEffectively()
    {
        var services = new ServiceCollection();
        services.AddNLWebNetMultiBackend();
        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        var concurrentRequests = 10;
        var testQuery = "performance test query";

        Console.WriteLine($"Testing concurrent load with {concurrentRequests} requests");

        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                var request = new NLWebRequest
                {
                    QueryId = $"concurrent-test-{i}",
                    Query = testQuery,
                    Mode = QueryMode.List
                };

                var taskStopwatch = Stopwatch.StartNew();
                var response = await nlWebService.ProcessRequestAsync(request);
                taskStopwatch.Stop();

                return new { Response = response, ElapsedMs = taskStopwatch.ElapsedMilliseconds };
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Verify all requests completed successfully
        foreach (var result in results)
        {
            Assert.IsNotNull(result.Response, "All concurrent requests should complete successfully");
            Assert.IsNull(result.Response.Error, "No concurrent requests should have errors");
        }

        var averageResponseTime = results.Average(r => r.ElapsedMs);
        var totalTime = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"Concurrent requests completed in {totalTime}ms");
        Console.WriteLine($"Average individual response time: {averageResponseTime:F2}ms");
        Console.WriteLine($"Throughput: {concurrentRequests * 1000.0 / totalTime:F2} requests/second");

        // Verify reasonable performance under load
        Assert.IsTrue(averageResponseTime <= 5000, // 5 second max per request under load
            $"Average response time under concurrent load ({averageResponseTime:F2}ms) should be reasonable");

        Console.WriteLine("✓ Concurrent load handling performance validated");
    }

    private async Task<BenchmarkResult> RunPerformanceBenchmark(PerformanceScenario scenario)
    {
        var services = new ServiceCollection();

        if (scenario.BackendCount > 1)
        {
            services.AddNLWebNetMultiBackend(options => { },
                multiOptions => multiOptions.Enabled = true);
        }
        else
        {
            services.AddNLWebNetMultiBackend();
        }

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        var responseTimes = new List<double>();
        var request = new NLWebRequest
        {
            Query = scenario.Query,
            Mode = QueryMode.List
        };

        // Warm up
        await nlWebService.ProcessRequestAsync(request);

        // Run benchmark iterations
        for (int i = 0; i < scenario.MinIterations; i++)
        {
            request.QueryId = $"perf-{scenario.Category}-{i}";

            var stopwatch = Stopwatch.StartNew();
            var response = await nlWebService.ProcessRequestAsync(request);
            stopwatch.Stop();

            Assert.IsNotNull(response, "Response should not be null during performance test");
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return new BenchmarkResult
        {
            ScenarioName = scenario.Name,
            AverageResponseTimeMs = responseTimes.Average(),
            MinResponseTimeMs = responseTimes.Min(),
            MaxResponseTimeMs = responseTimes.Max(),
            Percentile95Ms = CalculatePercentile(responseTimes, 95),
            IterationCount = responseTimes.Count
        };
    }

    private async Task<BenchmarkResult> BenchmarkConfiguration(string configName, bool enableMultiBackend)
    {
        var services = new ServiceCollection();

        if (enableMultiBackend)
        {
            services.AddNLWebNetMultiBackend(options => { },
                multiOptions => multiOptions.Enabled = true);
        }
        else
        {
            services.AddNLWebNetMultiBackend();
        }

        var serviceProvider = services.BuildServiceProvider();
        var nlWebService = serviceProvider.GetRequiredService<INLWebService>();

        var responseTimes = new List<double>();
        var testQuery = "benchmark configuration test";
        var iterations = 50;

        for (int i = 0; i < iterations; i++)
        {
            var request = new NLWebRequest
            {
                QueryId = $"config-{configName}-{i}",
                Query = testQuery,
                Mode = QueryMode.List
            };

            var stopwatch = Stopwatch.StartNew();
            await nlWebService.ProcessRequestAsync(request);
            stopwatch.Stop();

            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return new BenchmarkResult
        {
            ScenarioName = configName,
            AverageResponseTimeMs = responseTimes.Average(),
            MinResponseTimeMs = responseTimes.Min(),
            MaxResponseTimeMs = responseTimes.Max(),
            Percentile95Ms = CalculatePercentile(responseTimes, 95),
            IterationCount = iterations
        };
    }

    private static double CalculatePercentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);

        if (index == Math.Floor(index))
        {
            return sorted[(int)index];
        }

        var lower = sorted[(int)Math.Floor(index)];
        var upper = sorted[(int)Math.Ceiling(index)];
        return lower + (upper - lower) * (index - Math.Floor(index));
    }

    private static void GeneratePerformanceReport(List<BenchmarkResult> results)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("PERFORMANCE BENCHMARK REPORT");
        Console.WriteLine(new string('=', 60));

        foreach (var result in results)
        {
            Console.WriteLine($"\nScenario: {result.ScenarioName}");
            Console.WriteLine($"Iterations: {result.IterationCount}");
            Console.WriteLine($"Average: {result.AverageResponseTimeMs:F2}ms");
            Console.WriteLine($"Min: {result.MinResponseTimeMs:F2}ms");
            Console.WriteLine($"Max: {result.MaxResponseTimeMs:F2}ms");
            Console.WriteLine($"95th percentile: {result.Percentile95Ms:F2}ms");
        }

        Console.WriteLine("\n" + new string('=', 60));
    }
}

/// <summary>
/// Represents the result of a performance benchmark
/// </summary>
public class BenchmarkResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public double AverageResponseTimeMs { get; set; }
    public double MinResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public double Percentile95Ms { get; set; }
    public int IterationCount { get; set; }
}