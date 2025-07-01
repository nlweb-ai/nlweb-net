using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Extensions;

[TestClass]
public class MultiBackendExtensionsTests
{
    [TestMethod]
    public void AddNLWebNetMultiBackend_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNLWebNetMultiBackend(
            configureOptions: null,
            configureMultiBackend: options =>
            {
                options.Enabled = true;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that all required services are registered
        Assert.IsNotNull(serviceProvider.GetService<INLWebService>());
        Assert.IsNotNull(serviceProvider.GetService<IQueryProcessor>());
        Assert.IsNotNull(serviceProvider.GetService<IResultGenerator>());
        Assert.IsNotNull(serviceProvider.GetService<IBackendManager>());
        Assert.IsNotNull(serviceProvider.GetService<IDataBackend>());
    }

    [TestMethod]
    public void AddNLWebNetMultiBackend_WithMultiBackendDisabled_UsesBackwardCompatibility()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNLWebNetMultiBackend(
            configureOptions: null,
            configureMultiBackend: options =>
            {
                options.Enabled = false;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var multiBackendOptions = serviceProvider.GetRequiredService<IOptions<MultiBackendOptions>>();
        
        Assert.IsFalse(multiBackendOptions.Value.Enabled);
        
        // Should still be able to get the main service
        var nlWebService = serviceProvider.GetService<INLWebService>();
        Assert.IsNotNull(nlWebService);
    }

    [TestMethod]
    public void AddNLWebNetMultiBackend_ConfiguresMultiBackendOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNLWebNetMultiBackend(
            options => options.DefaultMode = QueryMode.Summarize,
            multiBackendOptions =>
            {
                multiBackendOptions.Enabled = true;
                multiBackendOptions.EnableParallelQuerying = false;
                multiBackendOptions.MaxConcurrentQueries = 3;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NLWebOptions>>();
        var multiBackendOptions = serviceProvider.GetRequiredService<IOptions<MultiBackendOptions>>();
        
        Assert.AreEqual(QueryMode.Summarize, options.Value.DefaultMode);
        Assert.IsTrue(multiBackendOptions.Value.Enabled);
        Assert.IsFalse(multiBackendOptions.Value.EnableParallelQuerying);
        Assert.AreEqual(3, multiBackendOptions.Value.MaxConcurrentQueries);
    }

    [TestMethod]
    public void AddNLWebNetMultiBackend_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNLWebNetMultiBackend();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NLWebOptions>>();
        var multiBackendOptions = serviceProvider.GetRequiredService<IOptions<MultiBackendOptions>>();
        
        // Should use default values
        Assert.AreEqual(QueryMode.List, options.Value.DefaultMode);
        Assert.IsFalse(multiBackendOptions.Value.Enabled); // Default is false for backward compatibility
        Assert.IsTrue(multiBackendOptions.Value.EnableParallelQuerying);
        Assert.AreEqual(5, multiBackendOptions.Value.MaxConcurrentQueries);
    }
}