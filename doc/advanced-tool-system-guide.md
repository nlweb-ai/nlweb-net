# Advanced Tool System Usage Guide

The Advanced Tool System provides enhanced query capabilities through specialized tool handlers that route queries to appropriate processors based on intent analysis.

## Configuration

Enable the tool system in your configuration:

```json
{
  "NLWebNet": {
    "ToolSelectionEnabled": true,
    "DefaultMode": "List"
  }
}
```

## Available Tools

### 1. Search Tool (`search`)
Enhanced keyword and semantic search with result optimization.

**Example Queries:**
- "search for REST API documentation"
- "find information about microservices"
- "locate best practices for authentication"

**Features:**
- Query optimization (removes redundant search terms)
- Enhanced relevance scoring
- Result sorting and filtering

### 2. Details Tool (`details`)
Retrieves comprehensive information about specific named items.

**Example Queries:**
- "tell me about GraphQL"
- "what is Docker?"
- "describe OAuth 2.0"
- "information about React hooks"

**Features:**
- Subject extraction from natural language queries
- Detailed information focus
- Comprehensive result filtering

### 3. Compare Tool (`compare`)
Side-by-side comparison of two items or technologies.

**Example Queries:**
- "compare React vs Vue"
- "difference between REST and GraphQL"
- "Node.js versus Python for backend"

**Features:**
- Automatic item extraction from comparison queries
- Structured comparison results
- Side-by-side analysis

### 4. Ensemble Tool (`ensemble`)
Creates cohesive sets of related recommendations.

**Example Queries:**
- "recommend a full-stack JavaScript development setup"
- "suggest tools for DevOps pipeline"
- "I need a complete testing framework"

**Features:**
- Multi-category recommendations
- Coherent item grouping
- Thematic organization

### 5. Recipe Tool (`recipe`)
Specialized for cooking, recipes, and food-related queries.

**Example Queries:**
- "substitute eggs in baking"
- "what goes with grilled salmon?"
- "recipe for chocolate chip cookies"

**Features:**
- Ingredient substitution guidance
- Food pairing suggestions
- Cooking technique advice

## Integration

The tool system integrates automatically with your existing NLWebNet setup:

```csharp
// Add NLWebNet services (includes tool system)
services.AddNLWebNet(options =>
{
    options.ToolSelectionEnabled = true;
});
```

## Backward Compatibility

When `ToolSelectionEnabled = false`, the system uses the standard query processing pipeline, maintaining full backward compatibility.

## Query Processing Flow

1. **Query Analysis**: The `IToolSelector` analyzes query intent
2. **Tool Selection**: Appropriate tool is selected based on keywords and patterns  
3. **Tool Execution**: The `IToolExecutor` routes to the selected tool handler
4. **Result Enhancement**: Each tool applies specialized processing
5. **Response Generation**: Results are formatted and returned

## Custom Tool Development

To create custom tools, implement the `IToolHandler` interface:

```csharp
public class CustomToolHandler : BaseToolHandler
{
    public override string ToolType => "custom";
    
    public override async Task<NLWebResponse> ExecuteAsync(
        NLWebRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Custom tool logic here
        return CreateSuccessResponse(request, results, processingTime);
    }
    
    public override bool CanHandle(NLWebRequest request)
    {
        // Determine if this tool can handle the request
        return request.Query.Contains("custom");
    }
}
```

Register your custom tool:

```csharp
services.AddScoped<IToolHandler, CustomToolHandler>();
```