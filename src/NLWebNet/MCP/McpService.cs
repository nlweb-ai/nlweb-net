using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.MCP;

/// <summary>
/// Implementation of the Model Context Protocol (MCP) service.
/// Provides tools and prompts for AI clients to interact with NLWeb functionality.
/// </summary>
public class McpService : IMcpService
{
    private readonly INLWebService _nlWebService;
    private readonly ILogger<McpService> _logger;

    public McpService(INLWebService nlWebService, ILogger<McpService> logger)
    {
        _nlWebService = nlWebService ?? throw new ArgumentNullException(nameof(nlWebService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<McpListToolsResponse> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing available MCP tools");

        var tools = new List<McpTool>
        {
            new()
            {
                Name = "nlweb_search",
                Description = "Search for information using natural language queries with support for different modes (list, summarize, generate)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The natural language query to search for"
                        },
                        mode = new
                        {
                            type = "string",
                            @enum = new[] { "list", "summarize", "generate" },
                            description = "Search mode: 'list' for results list, 'summarize' for summarized results, 'generate' for AI-generated responses",
                            @default = "list"
                        },
                        site = new
                        {
                            type = "string",
                            description = "Optional site filter to restrict search to specific data subset"
                        },
                        streaming = new
                        {
                            type = "boolean",
                            description = "Whether to enable streaming responses",
                            @default = true
                        }
                    },
                    required = new[] { "query" }
                }
            },
            new()
            {
                Name = "nlweb_query_history",
                Description = "Search using conversation history for contextual queries",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The current natural language query"
                        },
                        previous_queries = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Previous queries in the conversation for context"
                        },
                        mode = new
                        {
                            type = "string",
                            @enum = new[] { "list", "summarize", "generate" },
                            description = "Search mode",
                            @default = "list"
                        }
                    },
                    required = new[] { "query" }
                }
            }
        };

        return Task.FromResult(new McpListToolsResponse { Tools = tools });
    }

    /// <inheritdoc />
    public Task<McpListPromptsResponse> ListPromptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing available MCP prompts");

        var prompts = new List<McpPrompt>
        {
            new()
            {
                Name = "nlweb_search_prompt",
                Description = "Generate a well-structured search query for NLWeb",
                Arguments = new List<McpPromptArgument>
                {
                    new()
                    {
                        Name = "topic",
                        Description = "The main topic or subject to search for",
                        Required = true
                    },
                    new()
                    {
                        Name = "context",
                        Description = "Additional context or constraints for the search",
                        Required = false
                    }
                }
            },
            new()
            {
                Name = "nlweb_summarize_prompt",
                Description = "Create a prompt for summarizing search results",
                Arguments = new List<McpPromptArgument>
                {
                    new()
                    {
                        Name = "query",
                        Description = "The original search query",
                        Required = true
                    },
                    new()
                    {
                        Name = "result_count",
                        Description = "Number of results to summarize",
                        Required = false
                    }
                }
            },
            new()
            {
                Name = "nlweb_generate_prompt",
                Description = "Create a prompt for generating comprehensive answers from search results",
                Arguments = new List<McpPromptArgument>
                {
                    new()
                    {
                        Name = "question",
                        Description = "The question to answer using search results",
                        Required = true
                    },
                    new()
                    {
                        Name = "style",
                        Description = "Response style (detailed, concise, technical, etc.)",
                        Required = false
                    }
                }
            }
        };

        return Task.FromResult(new McpListPromptsResponse { Prompts = prompts });
    }

    /// <inheritdoc />
    public async Task<McpCallToolResponse> CallToolAsync(McpCallToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Calling MCP tool: {ToolName}", request.Name);

        try
        {
            switch (request.Name)
            {
                case "nlweb_search":
                    return await HandleNLWebSearchTool(request.Arguments, cancellationToken);

                case "nlweb_query_history":
                    return await HandleNLWebQueryHistoryTool(request.Arguments, cancellationToken);

                default:
                    _logger.LogWarning("Unknown MCP tool requested: {ToolName}", request.Name);
                    return new McpCallToolResponse
                    {
                        IsError = true,
                        Content = new List<McpContent>
                        {
                            new() { Type = "text", Text = $"Unknown tool: {request.Name}" }
                        }
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool {ToolName}", request.Name);
            return new McpCallToolResponse
            {
                IsError = true,
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"Error executing tool: {ex.Message}" }
                }
            };
        }
    }

    /// <inheritdoc />
    public Task<McpGetPromptResponse> GetPromptAsync(McpGetPromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Getting MCP prompt: {PromptName}", request.Name);

        switch (request.Name)
        {
            case "nlweb_search_prompt":
                return Task.FromResult(GenerateSearchPrompt(request.Arguments));

            case "nlweb_summarize_prompt":
                return Task.FromResult(GenerateSummarizePrompt(request.Arguments));

            case "nlweb_generate_prompt":
                return Task.FromResult(GenerateAnswerPrompt(request.Arguments));

            default:
                _logger.LogWarning("Unknown MCP prompt requested: {PromptName}", request.Name);
                return Task.FromResult(new McpGetPromptResponse
                {
                    Description = $"Unknown prompt: {request.Name}",
                    Messages = new List<McpPromptMessage>
                    {
                        new()
                        {
                            Role = "system",
                            Content = new McpContent { Type = "text", Text = $"Error: Unknown prompt '{request.Name}'" }
                        }
                    }
                });
        }
    }

    /// <inheritdoc />
    public async Task<NLWebResponse> ProcessNLWebQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Processing NLWeb query via MCP: {Query}", request.Query);

        // Use the existing NLWebService to process the query
        return await _nlWebService.ProcessRequestAsync(request, cancellationToken);
    }

    private async Task<McpCallToolResponse> HandleNLWebSearchTool(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        // Extract arguments
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        var mode = arguments.GetValueOrDefault("mode")?.ToString() ?? "list";
        var site = arguments.GetValueOrDefault("site")?.ToString();
        var streaming = arguments.GetValueOrDefault("streaming") as bool? ?? true;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new McpCallToolResponse
            {
                IsError = true,
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "Query parameter is required" }
                }
            };
        }

        // Create NLWeb request
        var nlWebRequest = new NLWebRequest
        {
            Query = query,
            Mode = Enum.TryParse<QueryMode>(mode, true, out var parsedMode) ? parsedMode : QueryMode.List,
            Site = site,
            Streaming = streaming
        };

        // Process the query
        var response = await _nlWebService.ProcessRequestAsync(nlWebRequest, cancellationToken);

        // Format response for MCP
        var resultText = FormatNLWebResponseForMcp(response);

        return new McpCallToolResponse
        {
            IsError = false,
            Content = new List<McpContent>
            {
                new() { Type = "text", Text = resultText }
            }
        };
    }

    private async Task<McpCallToolResponse> HandleNLWebQueryHistoryTool(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        // Extract arguments
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        var previousQueries = arguments.GetValueOrDefault("previous_queries") as IEnumerable<object>;
        var mode = arguments.GetValueOrDefault("mode")?.ToString() ?? "list";

        if (string.IsNullOrWhiteSpace(query))
        {
            return new McpCallToolResponse
            {
                IsError = true,
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "Query parameter is required" }
                }
            };
        }        // Create NLWeb request with history
        var nlWebRequest = new NLWebRequest
        {
            Query = query,
            Mode = Enum.TryParse<QueryMode>(mode, true, out var parsedMode) ? parsedMode : QueryMode.List,
            Prev = previousQueries != null ? string.Join(",", previousQueries.Select(q => q.ToString() ?? "").Where(q => !string.IsNullOrWhiteSpace(q))) : null
        };

        // Process the query
        var response = await _nlWebService.ProcessRequestAsync(nlWebRequest, cancellationToken);

        // Format response for MCP
        var resultText = FormatNLWebResponseForMcp(response);

        return new McpCallToolResponse
        {
            IsError = false,
            Content = new List<McpContent>
            {
                new() { Type = "text", Text = resultText }
            }
        };
    }

    private static string FormatNLWebResponseForMcp(NLWebResponse response)
    {
        var lines = new List<string>
        {
            $"Query ID: {response.QueryId}",
            $"Results Count: {response.Results.Count}",
            ""
        };

        if (!string.IsNullOrWhiteSpace(response.Summary))
        {
            lines.Add("Summary:");
            lines.Add(response.Summary);
            lines.Add("");
        }

        if (response.Results.Any())
        {
            lines.Add("Results:");
            for (int i = 0; i < response.Results.Count; i++)
            {
                var result = response.Results[i];
                lines.Add($"{i + 1}. {result.Name}");
                lines.Add($"   URL: {result.Url}");
                lines.Add($"   Score: {result.Score:F2}");
                if (!string.IsNullOrWhiteSpace(result.Description))
                {
                    lines.Add($"   Description: {result.Description}");
                }
                lines.Add("");
            }
        }

        return string.Join("\n", lines);
    }

    private static McpGetPromptResponse GenerateSearchPrompt(Dictionary<string, object> arguments)
    {
        var topic = arguments.GetValueOrDefault("topic")?.ToString() ?? "information";
        var context = arguments.GetValueOrDefault("context")?.ToString() ?? "";

        var promptText = $"Search for information about: {topic}";
        if (!string.IsNullOrWhiteSpace(context))
        {
            promptText += $"\n\nAdditional context: {context}";
        }

        return new McpGetPromptResponse
        {
            Description = "Structured search prompt for NLWeb",
            Messages = new List<McpPromptMessage>
            {
                new()
                {
                    Role = "user",
                    Content = new McpContent { Type = "text", Text = promptText }
                }
            }
        };
    }

    private static McpGetPromptResponse GenerateSummarizePrompt(Dictionary<string, object> arguments)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        var resultCount = arguments.GetValueOrDefault("result_count")?.ToString() ?? "multiple";

        var promptText = $"Please summarize the {resultCount} search results for the query: '{query}'. " +
                        "Provide a clear, concise summary that captures the key information from all results.";

        return new McpGetPromptResponse
        {
            Description = "Prompt for summarizing NLWeb search results",
            Messages = new List<McpPromptMessage>
            {
                new()
                {
                    Role = "system",
                    Content = new McpContent { Type = "text", Text = "You are a helpful assistant that summarizes search results clearly and concisely." }
                },
                new()
                {
                    Role = "user",
                    Content = new McpContent { Type = "text", Text = promptText }
                }
            }
        };
    }

    private static McpGetPromptResponse GenerateAnswerPrompt(Dictionary<string, object> arguments)
    {
        var question = arguments.GetValueOrDefault("question")?.ToString() ?? "";
        var style = arguments.GetValueOrDefault("style")?.ToString() ?? "detailed";

        var promptText = $"Based on the search results provided, please answer the following question in a {style} manner: {question}";

        return new McpGetPromptResponse
        {
            Description = "Prompt for generating comprehensive answers from search results",
            Messages = new List<McpPromptMessage>
            {
                new()
                {
                    Role = "system",
                    Content = new McpContent { Type = "text", Text = $"You are a knowledgeable assistant that provides {style} answers based on search results. Always cite your sources and be accurate." }
                },
                new()
                {
                    Role = "user",
                    Content = new McpContent { Type = "text", Text = promptText }
                }
            }
        };
    }
}
