namespace NLWebNet.Models;

/// <summary>
/// Configuration options for supporting different configuration formats (YAML, XML).
/// </summary>
public class ConfigurationFormatOptions
{
    /// <summary>
    /// Whether YAML configuration format is enabled.
    /// </summary>
    public bool EnableYamlSupport { get; set; } = true;

    /// <summary>
    /// Whether XML tool definitions are enabled.
    /// </summary>
    public bool EnableXmlToolDefinitions { get; set; } = true;

    /// <summary>
    /// Path to YAML configuration file for multi-backend configuration.
    /// If specified, this will be loaded in addition to standard configuration.
    /// </summary>
    public string? YamlConfigPath { get; set; }

    /// <summary>
    /// Path to XML tool definitions file.
    /// If specified, tool definitions will be loaded from this file.
    /// </summary>
    public string? XmlToolDefinitionsPath { get; set; }

    /// <summary>
    /// Whether to automatically detect and load configuration files based on naming conventions.
    /// Looks for files like 'config_retrieval.yaml' and 'tool_definitions.xml'.
    /// </summary>
    public bool AutoDetectConfigurationFiles { get; set; } = true;

    /// <summary>
    /// Base directory to search for configuration files when auto-detection is enabled.
    /// Defaults to the application's content root path.
    /// </summary>
    public string? ConfigurationBasePath { get; set; }
}