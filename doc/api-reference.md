# NLWebNet API Reference

This document provides detailed API reference information for the NLWebNet library, including endpoint specifications, request/response formats, and usage examples.

## Endpoints Overview

NLWebNet provides two main API endpoints that implement the NLWeb protocol:

- **`/ask`** - Primary NLWeb query endpoint
- **`/mcp`** - Model Context Protocol endpoint

## `/ask` Endpoint

The primary endpoint for natural language queries following the NLWeb protocol specification.

### HTTP Methods

- **GET** - Simple queries with URL parameters
- **POST** - Full featured queries with JSON body

### Request Parameters

#### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `query` | string | Natural language query string |

#### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `mode` | string | `"list"` | Query mode: `"list"`, `"summarize"`, or `"generate"` |
| `site` | string | `null` | Target site/domain subset for filtering |
| `prev` | string | `null` | Comma-separated previous query IDs for context |
| `decontextualized_query` | string | `null` | Pre-processed query (skips decontextualization) |
| `streaming` | boolean | `true` | Enable streaming responses via Server-Sent Events |
| `query_id` | string | auto-generated | Custom query identifier |

### Request Examples

#### GET Request


```http
GET /ask?query=find%20recent%20updates&mode=list&streaming=false

```

#### POST Request


```http
POST /ask
Content-Type: application/json

{
  "query": "what are the main features of this system?",
  "mode": "summarize",
  "site": "docs",
  "streaming": true,
  "query_id": "custom-query-123"
}

```

### Response Format

#### Standard Response (streaming=false)


```json
{
  "query_id": "550e8400-e29b-41d4-a716-446655440000",
  "query": "find recent updates",
  "decontextualized_query": "find recent system updates",
  "mode": "list",
  "results": [
    {
      "url": "https://example.com/updates",
      "title": "Recent System Updates",
      "snippet": "Latest updates and improvements...",
      "score": 0.95
    }
  ],
  "summary": null,
  "site": null,
  "generated_at": "2025-06-22T10:30:00Z"
}

```

#### Streaming Response (streaming=true)

Content-Type: `text/event-stream`


```text
data: {"type":"query_id","data":"550e8400-e29b-41d4-a716-446655440000"}

data: {"type":"decontextualized_query","data":"find recent system updates"}

data: {"type":"result","data":{"url":"https://example.com/updates","title":"Recent System Updates","snippet":"Latest updates...","score":0.95}}

data: {"type":"summary","data":"Based on the search results, recent updates include..."}

data: {"type":"complete","data":null}

```

### Query Modes

#### List Mode (`mode: "list"`)

Returns ranked search results without AI-generated summaries.


```json
{
  "query_id": "...",
  "query": "find documentation",
  "mode": "list",
  "results": [
    {
      "url": "https://example.com/docs",
      "title": "Documentation",
      "snippet": "Complete documentation...",
      "score": 0.92
    }
  ],
  "summary": null
}

```

#### Summarize Mode (`mode: "summarize"`)

Returns search results with an AI-generated summary.


```json
{
  "query_id": "...",
  "query": "what is the system architecture?",
  "mode": "summarize",
  "results": [
    {
      "url": "https://example.com/architecture",
      "title": "System Architecture",
      "snippet": "The system uses...",
      "score": 0.88
    }
  ],
  "summary": "The system architecture consists of three main layers..."
}

```

#### Generate Mode (`mode: "generate"`)

Returns a comprehensive AI-generated response using Retrieval-Augmented Generation (RAG).


```json
{
  "query_id": "...",
  "query": "how do I deploy the application?",
  "mode": "generate",
  "results": [
    {
      "url": "https://example.com/deployment",
      "title": "Deployment Guide",
      "snippet": "To deploy the application...",
      "score": 0.91
    }
  ],
  "summary": "To deploy the application, follow these steps: 1. Ensure prerequisites..."
}

```

## `/mcp` Endpoint

Model Context Protocol endpoint for enhanced AI integration and tool support.

### MCP HTTP Methods

- **POST** - All MCP operations

### Request Format


```http
POST /mcp
Content-Type: application/json

{
  "method": "list_tools",
  "params": {}
}

```

### Supported Methods

#### `list_tools`

Returns available tools for MCP integration.

**Request:**


```json
{
  "method": "list_tools",
  "params": {}
}

```

**Response:**


```json
{
  "tools": [
    {
      "name": "search",
      "description": "Search for relevant documents",
      "input_schema": {
        "type": "object",
        "properties": {
          "query": {"type": "string"}
        }
      }
    }
  ]
}

```

#### `list_prompts`

Returns available prompt templates.

**Request:**


```json
{
  "method": "list_prompts",
  "params": {}
}

```

**Response:**


```json
{
  "prompts": [
    {
      "name": "summarize",
      "description": "Summarize search results",
      "arguments": [
        {
          "name": "results",
          "description": "Search results to summarize"
        }
      ]
    }
  ]
}

```

#### `call_tool`

Execute a specific tool with parameters.

**Request:**


```json
{
  "method": "call_tool",
  "params": {
    "name": "search",
    "arguments": {
      "query": "recent updates"
    }
  }
}

```

**Response:**


```json
{
  "content": [
    {
      "type": "text",
      "text": "Found 5 results for 'recent updates'"
    }
  ]
}

```

#### `get_prompt`

Retrieve a specific prompt template.

**Request:**


```json
{
  "method": "get_prompt",
  "params": {
    "name": "summarize",
    "arguments": {
      "results": "[search results]"
    }
  }
}

```

**Response:**


```json
{
  "description": "Summarize the provided search results",
  "messages": [
    {
      "role": "user",
      "content": {
        "type": "text",
        "text": "Please summarize these search results: [search results]"
      }
    }
  ]
}

```

## Error Responses

### Standard Error Format


```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The query parameter is required.",
  "instance": "/ask"
}

```

### Common Error Codes

| Status Code | Title | Description |
|-------------|-------|-------------|
| 400 | Bad Request | Invalid request parameters or malformed JSON |
| 401 | Unauthorized | Authentication required (if auth is enabled) |
| 404 | Not Found | Endpoint not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected server error |
| 502 | Bad Gateway | External service (AI/data backend) unavailable |
| 503 | Service Unavailable | Service temporarily unavailable |

### Error Examples

#### Missing Required Parameter


```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The 'query' parameter is required.",
  "instance": "/ask"
}

```

#### Invalid Query Mode


```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid mode 'invalid'. Supported modes: list, summarize, generate.",
  "instance": "/ask"
}

```

#### AI Service Unavailable


```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.3",
  "title": "Bad Gateway",
  "status": 502,
  "detail": "AI service is currently unavailable. Please try again later.",
  "instance": "/ask"
}

```

## OpenAPI Specification

The complete OpenAPI specification is available at:

- **Development**: `http://localhost:5037/openapi/v1.json`
- **Swagger UI**: Available in the demo application at `/swagger`

## Rate Limiting

> **Note**: Rate limiting is configurable and may be disabled in development environments.

Default rate limits (when enabled):

- **Per IP**: 100 requests per minute
- **Per endpoint**: Separate limits may apply

Rate limit headers:


```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200

```

## Authentication

> **Note**: Authentication is optional and configurable based on deployment requirements.

When enabled, authentication can be configured for:

- **API Keys**: `Authorization: Bearer your-api-key`
- **OAuth 2.0**: Standard OAuth flows
- **Custom**: Application-specific authentication schemes

## Content Types

### Request Content Types

- `application/json` - For POST requests with JSON bodies
- `application/x-www-form-urlencoded` - For form-encoded data

### Response Content Types

- `application/json` - Standard JSON responses
- `text/event-stream` - For streaming responses
- `application/problem+json` - For error responses

## SDK and Client Libraries

Currently, NLWebNet provides:

- **.NET Library**: Available as NuGet package `NLWebNet`
- **HTTP API**: Standard REST endpoints for any HTTP client

Future client libraries may include:

- JavaScript/TypeScript SDK
- Python client library
- Additional language bindings

## Example Integration

### .NET Client


```csharp
using NLWebNet;

// Add to DI container
builder.Services.AddNLWebNet();

// Map endpoints
app.MapNLWebNet();

// Use in service or endpoint
public class MyService
{
    private readonly INLWebService _nlweb;

    public MyService(INLWebService nlweb)
    {
        _nlweb = nlweb;
    }

    public async Task<NLWebResponse> SearchAsync(string query)
    {
        var request = new NLWebRequest { Query = query };
        var response = await _nlweb.ProcessRequestAsync(request);
        return response;
    }
}

```

### cURL Examples


```bash

# Simple GET request

curl "http://localhost:5037/ask?query=hello&mode=list"

# POST with JSON

curl -X POST "http://localhost:5037/ask" \
  -H "Content-Type: application/json" \
  -d '{"query": "find documentation", "mode": "summarize"}'

# MCP tool listing

curl -X POST "http://localhost:5037/mcp" \
  -H "Content-Type: application/json" \
  -d '{"method": "list_tools", "params": {}}'

```

---

**Related Documentation:**

- [Demo Setup Guide](demo-setup-guide.md)
- [Manual Testing Guide](manual-testing-guide.md)
- [Development Guide](development-guide.md)
- [NLWeb Protocol Specification](https://github.com/microsoft/NLWeb)
