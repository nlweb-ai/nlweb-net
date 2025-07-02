using System.Xml.Serialization;

namespace NLWebNet.Models;

/// <summary>
/// Represents XML-based tool definitions for NLWeb tool selection framework.
/// </summary>
[XmlRoot("ToolDefinitions")]
public class ToolDefinitions
{
    /// <summary>
    /// Collection of tool definitions.
    /// </summary>
    [XmlElement("Tool")]
    public List<ToolDefinition> Tools { get; set; } = new();
}

/// <summary>
/// Represents a single tool definition in XML format.
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the tool.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of tool (e.g., "search", "details", "compare", "ensemble").
    /// </summary>
    [XmlAttribute("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this tool is enabled by default.
    /// </summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Priority for tool selection (higher values = higher priority).
    /// </summary>
    [XmlAttribute("priority")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    [XmlElement("Description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Configuration parameters for the tool.
    /// </summary>
    [XmlElement("Parameters")]
    public ToolParameters Parameters { get; set; } = new();

    /// <summary>
    /// Query patterns that should trigger this tool.
    /// </summary>
    [XmlArray("TriggerPatterns")]
    [XmlArrayItem("Pattern")]
    public List<string> TriggerPatterns { get; set; } = new();

    /// <summary>
    /// Backend types this tool supports.
    /// </summary>
    [XmlArray("SupportedBackends")]
    [XmlArrayItem("Backend")]
    public List<string> SupportedBackends { get; set; } = new();
}

/// <summary>
/// Represents tool configuration parameters.
/// </summary>
public class ToolParameters
{
    /// <summary>
    /// Maximum number of results this tool should return.
    /// </summary>
    [XmlElement("MaxResults")]
    public int MaxResults { get; set; } = 50;

    /// <summary>
    /// Timeout for tool execution in seconds.
    /// </summary>
    [XmlElement("TimeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable caching for this tool.
    /// </summary>
    [XmlElement("EnableCaching")]
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Custom configuration properties.
    /// </summary>
    [XmlArray("CustomProperties")]
    [XmlArrayItem("Property")]
    public List<CustomProperty> CustomProperties { get; set; } = new();
}

/// <summary>
/// Represents a custom configuration property.
/// </summary>
public class CustomProperty
{
    /// <summary>
    /// Property name.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Property value.
    /// </summary>
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Property data type.
    /// </summary>
    [XmlAttribute("type")]
    public string Type { get; set; } = "string";
}