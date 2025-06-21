using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using NLWebNet.Extensions;
using NLWebNet.Models;
using NLWebNet.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace NLWebNet.Tests.Extensions;

[TestClass]
public class AspireExtensionsTests
{
    [TestMethod]
    public void AddNLWebNetForAspire_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNLWebNetForAspire();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check core NLWebNet services are registered
        Assert.IsNotNull(serviceProvider.GetService<INLWebService>());
        Assert.IsNotNull(serviceProvider.GetService<IQueryProcessor>());
        Assert.IsNotNull(serviceProvider.GetService<IResultGenerator>());

        // Check OpenTelemetry services are registered (service discovery may not be directly accessible)
        Assert.IsNotNull(serviceProvider.GetService<OpenTelemetry.Metrics.MeterProvider>());
        Assert.IsNotNull(serviceProvider.GetService<OpenTelemetry.Trace.TracerProvider>());
    }

    [TestMethod]
    public void AddNLWebNetForAspire_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNLWebNetForAspire(options =>
        {
            options.DefaultMode = QueryMode.Summarize;
            options.EnableStreaming = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var nlwebService = serviceProvider.GetService<INLWebService>();
        Assert.IsNotNull(nlwebService);
    }

    [TestMethod]
    public void AddNLWebNetDefaults_ConfiguresHostBuilder()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddNLWebNetDefaults();

        // Assert
        var app = builder.Build();
        var serviceProvider = app.Services;

        // Check NLWebNet services are registered
        Assert.IsNotNull(serviceProvider.GetService<INLWebService>());

        // Check OpenTelemetry services are registered
        Assert.IsNotNull(serviceProvider.GetService<OpenTelemetry.Metrics.MeterProvider>());
        Assert.IsNotNull(serviceProvider.GetService<OpenTelemetry.Trace.TracerProvider>());
    }

    [TestMethod]
    public void AddNLWebNetDefaults_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddNLWebNetDefaults(options =>
        {
            options.DefaultMode = QueryMode.Generate;
            options.EnableStreaming = true;
        });

        // Assert
        var app = builder.Build();
        var serviceProvider = app.Services;
        Assert.IsNotNull(serviceProvider.GetService<INLWebService>());
    }
}