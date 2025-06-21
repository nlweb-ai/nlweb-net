using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLWebNet.Extensions;
using NLWebNet.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NLWebNet.Tests.Extensions;

[TestClass]
public class OpenTelemetryExtensionsTests
{
    [TestMethod]
    public void AddNLWebNetOpenTelemetry_WithDefaults_RegistersOpenTelemetryServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNLWebNetOpenTelemetry();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        Assert.IsNotNull(meterProvider);
        Assert.IsNotNull(tracerProvider);
    }

    [TestMethod]
    public void AddNLWebNetOpenTelemetry_WithCustomServiceName_ConfiguresResource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        const string serviceName = "TestService";
        const string serviceVersion = "2.0.0";

        // Act
        services.AddNLWebNetOpenTelemetry(serviceName, serviceVersion);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.IsNotNull(tracerProvider);
    }

    [TestMethod]
    public void AddNLWebNetOpenTelemetry_WithCustomConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configurationApplied = false;

        // Act
        services.AddNLWebNetOpenTelemetry("TestService", "1.0.0", builder =>
        {
            configurationApplied = true;
            builder.AddConsoleExporters();
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.IsNotNull(tracerProvider);
        Assert.IsTrue(configurationApplied);
    }

    [TestMethod]
    public void AddConsoleExporters_ConfiguresConsoleExporters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddOpenTelemetry();

        // Act
        builder.AddConsoleExporters();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        Assert.IsNotNull(meterProvider);
        Assert.IsNotNull(tracerProvider);
    }

    [TestMethod]
    public void AddOtlpExporters_ConfiguresOtlpExporters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddOpenTelemetry();

        // Act
        builder.AddOtlpExporters("http://localhost:4317");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        Assert.IsNotNull(meterProvider);
        Assert.IsNotNull(tracerProvider);
    }

    [TestMethod]
    public void AddPrometheusExporter_ConfiguresPrometheusExporter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddOpenTelemetry();

        // Act
        builder.AddPrometheusExporter();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        Assert.IsNotNull(meterProvider);
    }

    [TestMethod]
    public void ConfigureForAspire_ConfiguresAspireSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddOpenTelemetry();

        // Act
        builder.ConfigureForAspire();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        Assert.IsNotNull(meterProvider);
        Assert.IsNotNull(tracerProvider);
    }
}