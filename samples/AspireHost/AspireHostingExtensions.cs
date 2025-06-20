using Aspire.Hosting;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for adding NLWebNet to Aspire host projects
/// Note: This file should only be used in projects that reference Aspire.Hosting packages
/// </summary>
public static class AspireHostingExtensions
{
    /// <summary>
    /// Adds an NLWebNet application to the Aspire host
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <param name="name">The name of the application</param>
    /// <returns>A resource builder for the NLWebNet application</returns>
    public static IResourceBuilder<ProjectResource> AddNLWebNetApp(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        return builder.AddProject<Projects.NLWebNet_Demo>(name)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEnvironment("OTEL_SERVICE_NAME", name)
            .WithEnvironment("OTEL_SERVICE_VERSION", "1.0.0");
    }

    /// <summary>
    /// Adds an NLWebNet application with custom configuration to the Aspire host
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <param name="name">The name of the application</param>
    /// <param name="configure">Configuration callback for the resource</param>
    /// <returns>A resource builder for the NLWebNet application</returns>
    public static IResourceBuilder<ProjectResource> AddNLWebNetApp(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<IResourceBuilder<ProjectResource>> configure)
    {
        var resource = builder.AddNLWebNetApp(name);
        configure(resource);
        return resource;
    }

    /// <summary>
    /// Adds an NLWebNet application with external data backend reference
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <param name="name">The name of the application</param>
    /// <param name="dataBackend">The data backend resource to reference</param>
    /// <returns>A resource builder for the NLWebNet application</returns>
    public static IResourceBuilder<ProjectResource> AddNLWebNetAppWithDataBackend(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<IResourceWithConnectionString> dataBackend)
    {
        return builder.AddNLWebNetApp(name)
            .WithReference(dataBackend)
            .WithEnvironment("NLWebNet__DataBackend__ConnectionString", dataBackend.Resource.GetConnectionString());
    }
}