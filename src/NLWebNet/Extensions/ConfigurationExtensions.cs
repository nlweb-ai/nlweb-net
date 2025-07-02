using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using NLWebNet.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NLWebNet.Extensions;

/// <summary>
/// Extension methods for adding YAML and XML configuration support to NLWebNet.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds a YAML configuration file to the configuration builder.
    /// Supports the new YAML-style multi-backend configuration format.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">Path to the YAML configuration file.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether to reload when the file changes.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        return builder.AddYamlFile(provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
    }

    /// <summary>
    /// Adds a YAML configuration file to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="provider">The file provider to use to access the file.</param>
    /// <param name="path">Path to the YAML configuration file.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether to reload when the file changes.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        IFileProvider? provider,
        string path,
        bool optional,
        bool reloadOnChange)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        return builder.Add<YamlConfigurationSource>(s =>
        {
            s.FileProvider = provider;
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            s.ResolveFileProvider();
        });
    }

    /// <summary>
    /// Adds YAML configuration stream to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="stream">The stream containing YAML configuration.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddYamlStream(this IConfigurationBuilder builder, Stream stream)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.Add<YamlStreamConfigurationSource>(s => s.Stream = stream);
    }

    /// <summary>
    /// Adds NLWeb configuration format support including YAML and XML tool definitions.
    /// This method should be called during application startup to enable advanced configuration formats.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNLWebConfigurationFormats(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the tool definition loader service
        services.AddSingleton<IToolDefinitionLoader, ToolDefinitionLoader>();

        return services;
    }

    /// <summary>
    /// Configures the configuration builder to automatically load YAML and XML configuration files.
    /// This method scans for standard NLWeb configuration files and loads them if found.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <param name="basePath">Optional base path to search for configuration files.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddNLWebConfigurationFormats(this IConfigurationBuilder builder,
        IHostEnvironment? environment = null,
        string? basePath = null)
    {
        var searchPath = basePath ?? environment?.ContentRootPath ?? Directory.GetCurrentDirectory();

        // Look for standard YAML configuration files
        var yamlFiles = new[]
        {
            "config_retrieval.yaml",
            "config_retrieval.yml",
            "nlweb.yaml",
            "nlweb.yml"
        };

        foreach (var yamlFile in yamlFiles)
        {
            var fullPath = Path.Combine(searchPath, yamlFile);
            if (File.Exists(fullPath))
            {
                builder.AddYamlFile(yamlFile, optional: true, reloadOnChange: true);
                break; // Use the first one found
            }
        }

        // Add environment-specific YAML files
        if (environment != null)
        {
            var envYamlFiles = new[]
            {
                $"config_retrieval.{environment.EnvironmentName}.yaml",
                $"config_retrieval.{environment.EnvironmentName}.yml",
                $"nlweb.{environment.EnvironmentName}.yaml",
                $"nlweb.{environment.EnvironmentName}.yml"
            };

            foreach (var envYamlFile in envYamlFiles)
            {
                var fullPath = Path.Combine(searchPath, envYamlFile);
                if (File.Exists(fullPath))
                {
                    builder.AddYamlFile(envYamlFile, optional: true, reloadOnChange: true);
                    break; // Use the first one found
                }
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a configuration builder with NLWeb configuration format support.
    /// This is a convenience method that sets up YAML and other configuration providers.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>A configured configuration builder.</returns>
    public static IConfigurationBuilder CreateNLWebConfiguration(string[]? args = null,
        IHostEnvironment? environment = null)
    {
        var builder = new ConfigurationBuilder();

        // Add standard configuration sources
        builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        if (environment != null)
        {
            builder.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        }

        // Add NLWeb-specific configuration formats
        builder.AddNLWebConfigurationFormats(environment);

        // Add environment variables and command line
        builder.AddEnvironmentVariables();

        if (args != null && args.Length > 0)
        {
            builder.AddCommandLine(args);
        }

        return builder;
    }
}

/// <summary>
/// Represents a YAML file as an IConfigurationSource.
/// </summary>
public class YamlConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Builds the YamlConfigurationProvider for this source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>A YamlConfigurationProvider.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new YamlConfigurationProvider(this);
    }
}

/// <summary>
/// A YAML file based configuration provider.
/// </summary>
public class YamlConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance with the specified source.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public YamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

    /// <summary>
    /// Loads the YAML data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    public override void Load(Stream stream)
    {
        try
        {
            Data = YamlConfigurationFileParser.Parse(stream);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Could not parse the YAML file. Error on line {GetLineNumber(ex)}: {ex.Message}", ex);
        }
    }

    private static int GetLineNumber(Exception ex)
    {
        // Try to extract line number from YamlDotNet exceptions
        if (ex.Message.Contains("line"))
        {
            var parts = ex.Message.Split(' ');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(':', ',', '.'), out int lineNumber))
                {
                    return lineNumber;
                }
            }
        }
        return 0;
    }
}

/// <summary>
/// Represents a YAML stream as an IConfigurationSource.
/// </summary>
public class YamlStreamConfigurationSource : StreamConfigurationSource
{
    /// <summary>
    /// Builds the YamlStreamConfigurationProvider for this source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>A YamlStreamConfigurationProvider.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new YamlStreamConfigurationProvider(this);
    }
}

/// <summary>
/// A YAML stream based configuration provider.
/// </summary>
public class YamlStreamConfigurationProvider : StreamConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance with the specified source.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public YamlStreamConfigurationProvider(YamlStreamConfigurationSource source) : base(source) { }

    /// <summary>
    /// Loads the YAML data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    public override void Load(Stream stream)
    {
        Data = YamlConfigurationFileParser.Parse(stream);
    }
}

/// <summary>
/// Helper class for parsing YAML configuration files.
/// </summary>
internal static class YamlConfigurationFileParser
{
    /// <summary>
    /// Parses a YAML stream into a configuration dictionary.
    /// </summary>
    /// <param name="stream">The stream containing YAML data.</param>
    /// <returns>A dictionary of configuration key-value pairs.</returns>
    public static IDictionary<string, string?> Parse(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        var yamlContent = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            return data;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yamlObject = deserializer.Deserialize(yamlContent);

        if (yamlObject != null)
        {
            FlattenYamlObject(string.Empty, yamlObject, data);
        }

        return data;
    }

    private static void FlattenYamlObject(string prefix, object value, IDictionary<string, string?> data)
    {
        switch (value)
        {
            case Dictionary<object, object> dict:
                foreach (var kvp in dict)
                {
                    var key = kvp.Key?.ToString() ?? string.Empty;
                    var newPrefix = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
                    FlattenYamlObject(newPrefix, kvp.Value, data);
                }
                break;

            case List<object> list:
                for (int i = 0; i < list.Count; i++)
                {
                    var newPrefix = $"{prefix}:{i}";
                    FlattenYamlObject(newPrefix, list[i], data);
                }
                break;

            default:
                var effectivePrefix = string.IsNullOrEmpty(prefix) ? "root" : prefix;
                data[effectivePrefix] = value?.ToString();
                break;
        }
    }
}