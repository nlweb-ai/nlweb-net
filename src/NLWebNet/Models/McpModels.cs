using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NLWebNet.Models;

/// <summary>
/// MCP Tool metadata for tool listing.
/// </summary>
public class McpTool
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON schema describing the input parameters for the tool.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object? InputSchema { get; set; }
}

/// <summary>
/// MCP Prompt metadata for prompt listing.
/// </summary>
public class McpPrompt
{
    /// <summary>
    /// Unique identifier for the prompt.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the prompt.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of argument names that the prompt accepts.
    /// </summary>
    [JsonPropertyName("arguments")]
    public List<McpPromptArgument> Arguments { get; set; } = new();
}

/// <summary>
/// MCP Prompt argument definition.
/// </summary>
public class McpPromptArgument
{
    /// <summary>
    /// Name of the argument.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the argument.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the argument is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

/// <summary>
/// Response for MCP list_tools method.
/// </summary>
public class McpListToolsResponse
{
    /// <summary>
    /// List of available tools.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}

/// <summary>
/// Response for MCP list_prompts method.
/// </summary>
public class McpListPromptsResponse
{
    /// <summary>
    /// List of available prompts.
    /// </summary>
    [JsonPropertyName("prompts")]
    public List<McpPrompt> Prompts { get; set; } = new();
}

/// <summary>
/// Request for MCP call_tool method.
/// </summary>
public class McpCallToolRequest
{
    /// <summary>
    /// Name of the tool to call.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Response for MCP call_tool method.
/// </summary>
public class McpCallToolResponse
{
    /// <summary>
    /// Content returned by the tool.
    /// </summary>
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();

    /// <summary>
    /// Whether the tool call was successful.
    /// </summary>
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

/// <summary>
/// Request for MCP get_prompt method.
/// </summary>
public class McpGetPromptRequest
{
    /// <summary>
    /// Name of the prompt to retrieve.
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Arguments for prompt template substitution.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Response for MCP get_prompt method.
/// </summary>
public class McpGetPromptResponse
{
    /// <summary>
    /// Description of the prompt.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Messages that make up the prompt.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<McpPromptMessage> Messages { get; set; } = new();
}

/// <summary>
/// Content item for MCP responses.
/// </summary>
public class McpContent
{
    /// <summary>
    /// Type of content (text, image, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Message in an MCP prompt.
/// </summary>
public class McpPromptMessage
{
    /// <summary>
    /// Role of the message (user, assistant, system).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public McpContent Content { get; set; } = new();
}
