using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLWebNet.Models;

namespace NLWebNet.MCP;

/// <summary>
/// Interface for Model Context Protocol (MCP) service implementation.
/// Provides core MCP methods for tools and prompts management.
/// </summary>
public interface IMcpService
{
    /// <summary>
    /// Lists all available tools that can be called via MCP.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of available tools with their metadata.</returns>
    Task<McpListToolsResponse> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available prompts that can be retrieved via MCP.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of available prompts with their metadata.</returns>
    Task<McpListPromptsResponse> ListPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a specific tool with the provided arguments.
    /// </summary>
    /// <param name="request">Tool call request containing tool name and arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tool execution result.</returns>
    Task<McpCallToolResponse> CallToolAsync(McpCallToolRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific prompt with optional argument substitution.
    /// </summary>
    /// <param name="request">Prompt request containing prompt name and arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Prompt content with substituted arguments.</returns>
    Task<McpGetPromptResponse> GetPromptAsync(McpGetPromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an NLWeb query and returns the response in MCP format.
    /// </summary>
    /// <param name="request">NLWeb request to process.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>NLWeb response formatted for MCP consumption.</returns>
    Task<NLWebResponse> ProcessNLWebQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default);
}
