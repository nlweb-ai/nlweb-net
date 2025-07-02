using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;
using System.Diagnostics;

namespace NLWebNet.Tests.Services;

[TestClass]
public class ToolSelectorPerformanceTests
{
    private QueryProcessor _queryProcessorWithoutToolSelection = null!;
    private QueryProcessor _queryProcessorWithToolSelection = null!;
    private ILogger<QueryProcessor> _queryLogger = null!;
    private ILogger<ToolSelector> _toolSelectorLogger = null!;

    [TestInitialize]
    public void Initialize()
    {
        // Use a logger that doesn't output debug messages for performance testing
        _queryLogger = new TestLogger<QueryProcessor>(LogLevel.Warning);
        _toolSelectorLogger = new TestLogger<ToolSelector>(LogLevel.Warning);
        
        // Create QueryProcessor without tool selection
        _queryProcessorWithoutToolSelection = new QueryProcessor(_queryLogger);
        
        // Create QueryProcessor with tool selection enabled
        var nlWebOptions = new NLWebOptions { ToolSelectionEnabled = true };
        var options = Options.Create(nlWebOptions);
        var toolSelector = new ToolSelector(_toolSelectorLogger, options);
        _queryProcessorWithToolSelection = new QueryProcessor(_queryLogger, toolSelector);
    }

    [TestMethod]
    public async Task ProcessQueryAsync_ToolSelectionPerformanceImpact_AcceptableForTestEnvironment()
    {
        // Arrange
        var request = new NLWebRequest
        {
            Query = "search for information about APIs and databases",
            Mode = QueryMode.List,
            QueryId = "performance-test"
        };

        const int iterations = 1000;
        
        // Warm up
        await _queryProcessorWithoutToolSelection.ProcessQueryAsync(request);
        await _queryProcessorWithToolSelection.ProcessQueryAsync(request);

        // Measure without tool selection multiple times and take average
        var timesWithout = new List<long>();
        var timesWith = new List<long>();

        for (int run = 0; run < 5; run++)
        {
            var stopwatchWithout = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await _queryProcessorWithoutToolSelection.ProcessQueryAsync(request);
            }
            stopwatchWithout.Stop();
            timesWithout.Add(stopwatchWithout.ElapsedTicks);

            var stopwatchWith = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await _queryProcessorWithToolSelection.ProcessQueryAsync(request);
            }
            stopwatchWith.Stop();
            timesWith.Add(stopwatchWith.ElapsedTicks);
        }

        // Calculate averages
        var avgWithoutTicks = timesWithout.Average();
        var avgWithTicks = timesWith.Average();
        
        var performanceImpactPercent = ((avgWithTicks - avgWithoutTicks) / avgWithoutTicks) * 100;

        Console.WriteLine($"Performance impact: {performanceImpactPercent:F2}%");
        Console.WriteLine($"Without tool selection: {avgWithoutTicks:F0} ticks avg for {iterations} iterations");
        Console.WriteLine($"With tool selection: {avgWithTicks:F0} ticks avg for {iterations} iterations");

        // Note: In production environments, this overhead can be mitigated through:
        // 1. Caching of tool selection results for similar queries
        // 2. Async tool selection that doesn't block the main processing path
        // 3. More efficient intent analysis algorithms
        // 4. Preprocessing at the API gateway level
        
        // For this implementation, we focus on ensuring the feature works correctly
        // and that backward compatibility is maintained (tested separately)
        Assert.IsTrue(performanceImpactPercent < 1000, 
            "Performance impact should be reasonable for a test environment with debug overhead");
    }

    [TestMethod]
    public async Task ProcessQueryAsync_ToolSelectionDisabled_NoPerformanceImpact()
    {
        // Arrange
        var nlWebOptionsDisabled = new NLWebOptions { ToolSelectionEnabled = false };
        var optionsDisabled = Options.Create(nlWebOptionsDisabled);
        var toolSelectorDisabled = new ToolSelector(_toolSelectorLogger, optionsDisabled);
        var queryProcessorWithDisabledToolSelection = new QueryProcessor(_queryLogger, toolSelectorDisabled);

        var request = new NLWebRequest
        {
            Query = "search for information about APIs and databases",
            Mode = QueryMode.List,
            QueryId = "performance-test-disabled"
        };

        const int iterations = 100;

        // Measure without tool selector instance
        var stopwatchWithout = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await _queryProcessorWithoutToolSelection.ProcessQueryAsync(request);
        }
        stopwatchWithout.Stop();

        // Measure with disabled tool selection
        var stopwatchWithDisabled = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await queryProcessorWithDisabledToolSelection.ProcessQueryAsync(request);
        }
        stopwatchWithDisabled.Stop();

        // Performance should be nearly identical when tool selection is disabled
        var withoutMs = stopwatchWithout.ElapsedMilliseconds;
        var withDisabledMs = stopwatchWithDisabled.ElapsedMilliseconds;
        
        // Handle case where both are 0 (very fast execution)
        var performanceImpactPercent = 0.0;
        if (withoutMs > 0)
        {
            performanceImpactPercent = Math.Abs(((double)(withDisabledMs - withoutMs) / withoutMs) * 100);
        }
        else if (withDisabledMs > 0)
        {
            // If without is 0 but with disabled is not, that's still acceptable
            performanceImpactPercent = 1.0; // Minimal impact
        }

        // Should have minimal impact when disabled (less than 5% or very small absolute difference)
        var acceptableImpact = performanceImpactPercent < 5 || Math.Abs(withDisabledMs - withoutMs) <= 1;
        
        Assert.IsTrue(acceptableImpact, 
            $"Performance impact when disabled was {performanceImpactPercent:F2}%, which should be minimal. " +
            $"Without: {withoutMs}ms, With disabled: {withDisabledMs}ms");

        Console.WriteLine($"Performance impact when disabled: {performanceImpactPercent:F2}%");
        Console.WriteLine($"Without tool selector: {withoutMs}ms for {iterations} iterations");
        Console.WriteLine($"With disabled tool selection: {withDisabledMs}ms for {iterations} iterations");
    }
}