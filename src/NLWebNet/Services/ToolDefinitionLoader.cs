using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using System.Xml;
using System.Xml.Serialization;

namespace NLWebNet.Services;

/// <summary>
/// Interface for loading XML-based tool definitions.
/// </summary>
public interface IToolDefinitionLoader
{
    /// <summary>
    /// Loads tool definitions from an XML file.
    /// </summary>
    /// <param name="filePath">Path to the XML file containing tool definitions.</param>
    /// <returns>The loaded tool definitions.</returns>
    Task<ToolDefinitions> LoadFromFileAsync(string filePath);

    /// <summary>
    /// Loads tool definitions from an XML string.
    /// </summary>
    /// <param name="xmlContent">XML content containing tool definitions.</param>
    /// <returns>The loaded tool definitions.</returns>
    ToolDefinitions LoadFromXml(string xmlContent);

    /// <summary>
    /// Loads tool definitions from a stream.
    /// </summary>
    /// <param name="stream">Stream containing XML tool definitions.</param>
    /// <returns>The loaded tool definitions.</returns>
    ToolDefinitions LoadFromStream(Stream stream);

    /// <summary>
    /// Validates tool definitions and returns any validation errors.
    /// </summary>
    /// <param name="toolDefinitions">Tool definitions to validate.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    IEnumerable<string> ValidateToolDefinitions(ToolDefinitions toolDefinitions);
}

/// <summary>
/// Service for loading and managing XML-based tool definitions.
/// </summary>
public class ToolDefinitionLoader : IToolDefinitionLoader
{
    private readonly ILogger<ToolDefinitionLoader> _logger;
    private readonly XmlSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the ToolDefinitionLoader.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ToolDefinitionLoader(ILogger<ToolDefinitionLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = new XmlSerializer(typeof(ToolDefinitions));
    }

    /// <inheritdoc />
    public async Task<ToolDefinitions> LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Tool definition file not found: {filePath}");

        try
        {
            _logger.LogInformation("Loading tool definitions from file: {FilePath}", filePath);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var toolDefinitions = await Task.Run(() => LoadFromStream(fileStream));

            _logger.LogInformation("Successfully loaded {ToolCount} tool definitions from {FilePath}",
                toolDefinitions.Tools.Count, filePath);

            return toolDefinitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tool definitions from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public ToolDefinitions LoadFromXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

        try
        {
            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            });

            var result = (ToolDefinitions?)_serializer.Deserialize(xmlReader);
            var toolDefinitions = result ?? new ToolDefinitions();

            // Validate the loaded definitions
            var validationErrors = ValidateToolDefinitions(toolDefinitions).ToList();
            if (validationErrors.Any())
            {
                var errorMessage = $"Tool definitions validation failed: {string.Join("; ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return toolDefinitions;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Failed to deserialize tool definitions from XML content");
            throw new InvalidOperationException("Failed to parse tool definitions XML", ex);
        }
    }

    /// <inheritdoc />
    public ToolDefinitions LoadFromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        try
        {
            using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            });

            var result = (ToolDefinitions?)_serializer.Deserialize(xmlReader);
            var toolDefinitions = result ?? new ToolDefinitions();

            // Validate the loaded definitions
            var validationErrors = ValidateToolDefinitions(toolDefinitions).ToList();
            if (validationErrors.Any())
            {
                var errorMessage = $"Tool definitions validation failed: {string.Join("; ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return toolDefinitions;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Failed to deserialize tool definitions from stream");
            throw new InvalidOperationException("Failed to parse tool definitions XML", ex);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> ValidateToolDefinitions(ToolDefinitions toolDefinitions)
    {
        if (toolDefinitions == null)
        {
            yield return "Tool definitions cannot be null";
            yield break;
        }

        if (toolDefinitions.Tools == null)
        {
            yield return "Tools collection cannot be null";
            yield break;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < toolDefinitions.Tools.Count; i++)
        {
            var tool = toolDefinitions.Tools[i];
            var prefix = $"Tool {i + 1}";

            if (string.IsNullOrWhiteSpace(tool.Id))
            {
                yield return $"{prefix}: Tool ID cannot be empty";
                continue;
            }

            if (!seenIds.Add(tool.Id))
            {
                yield return $"{prefix}: Duplicate tool ID '{tool.Id}'";
            }

            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                yield return $"{prefix} (ID: {tool.Id}): Tool name cannot be empty";
            }

            if (string.IsNullOrWhiteSpace(tool.Type))
            {
                yield return $"{prefix} (ID: {tool.Id}): Tool type cannot be empty";
            }
            else if (!IsValidToolType(tool.Type))
            {
                yield return $"{prefix} (ID: {tool.Id}): Invalid tool type '{tool.Type}'. Valid types are: search, details, compare, ensemble";
            }

            if (tool.Priority < 0)
            {
                yield return $"{prefix} (ID: {tool.Id}): Priority cannot be negative";
            }

            if (tool.Parameters != null)
            {
                if (tool.Parameters.MaxResults <= 0)
                {
                    yield return $"{prefix} (ID: {tool.Id}): MaxResults must be greater than 0";
                }

                if (tool.Parameters.TimeoutSeconds <= 0)
                {
                    yield return $"{prefix} (ID: {tool.Id}): TimeoutSeconds must be greater than 0";
                }

                // Validate custom properties
                if (tool.Parameters.CustomProperties != null)
                {
                    var seenPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in tool.Parameters.CustomProperties)
                    {
                        if (string.IsNullOrWhiteSpace(property.Name))
                        {
                            yield return $"{prefix} (ID: {tool.Id}): Custom property name cannot be empty";
                            continue;
                        }

                        if (!seenPropertyNames.Add(property.Name))
                        {
                            yield return $"{prefix} (ID: {tool.Id}): Duplicate custom property name '{property.Name}'";
                        }
                    }
                }
            }
        }
    }

    private static bool IsValidToolType(string toolType)
    {
        var validTypes = new[] { "search", "details", "compare", "ensemble" };
        return validTypes.Contains(toolType.ToLowerInvariant());
    }
}