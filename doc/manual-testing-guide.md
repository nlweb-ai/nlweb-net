# Manual Testing Guide for NLWebNet API

This guide provides sample requests for manual testing of the NLWebNet API endpoints.

## Prerequisites

1. Start the demo application:


```bash
cd demo
dotnet run

```

The application will be available at `http://localhost:5037`

## Testing the /ask Endpoint

### 1. Basic GET Request - List Mode


```bash
curl "http://localhost:5037/ask?query=artificial%20intelligence&mode=List"

```

### 2. POST Request with JSON Body - Summarize Mode


```bash
curl -X POST "http://localhost:5037/ask" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning algorithms",
    "mode": "Summarize",
    "site": "example",
    "streaming": false
  }'

```

### 3. Generate Mode with Previous Context


```bash
curl -X POST "http://localhost:5037/ask" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What are the latest developments?",
    "mode": "Generate",
    "prev": ["artificial intelligence", "machine learning"],
    "decontextualized_query": "What are the latest developments in artificial intelligence and machine learning?",
    "streaming": false
  }'

```

### 4. Streaming Request


```bash
curl "http://localhost:5037/ask?query=deep%20learning&streaming=true"

```

## Testing the /mcp Endpoint

### 1. Basic MCP Request


```bash
curl -X POST "http://localhost:5037/mcp" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "neural networks",
    "mode": "List",
    "streaming": false
  }'

```

### 2. MCP with Site Filter


```bash
curl -X POST "http://localhost:5037/mcp" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "computer vision",
    "mode": "Summarize",
    "site": "research",
    "streaming": false
  }'

```

## Testing MCP Protocol Endpoints

### 1. List Available Tools


```bash
curl "http://localhost:5037/mcp/list_tools"

```

### 2. List Available Prompts


```bash
curl "http://localhost:5037/mcp/list_prompts"

```

### 3. Call nlweb_search Tool


```bash
curl -X POST "http://localhost:5037/mcp/call_tool" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "nlweb_search",
    "arguments": {
      "query": "natural language processing",
      "mode": "list",
      "site": "papers"
    }
  }'

```

### 4. Call nlweb_query_history Tool


```bash
curl -X POST "http://localhost:5037/mcp/call_tool" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "nlweb_query_history",
    "arguments": {
      "query": "What are transformers?",
      "previous_queries": ["machine learning", "neural networks", "deep learning"]
    }
  }'

```

### 5. Get Search Prompt


```bash
curl -X POST "http://localhost:5037/mcp/get_prompt" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "nlweb_search_prompt",
    "arguments": {
      "query": "quantum computing",
      "context": "research papers"
    }
  }'

```

### 6. Get Summarize Prompt


```bash
curl -X POST "http://localhost:5037/mcp/get_prompt" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "nlweb_summarize_prompt",
    "arguments": {
      "results": "[{\"title\": \"Quantum Computing Basics\", \"content\": \"...\"}]",
      "query": "quantum computing fundamentals"
    }
  }'

```

## Testing OpenAPI Documentation

### 1. Get OpenAPI Schema


```bash
curl "http://localhost:5037/openapi/v1.json"

```

## Error Testing

### 1. Missing Query Parameter


```bash
curl "http://localhost:5037/ask"

# Expected: 400 Bad Request


```

### 2. Invalid Mode


```bash
curl "http://localhost:5037/ask?query=test&mode=InvalidMode"

# Expected: 400 Bad Request (depending on validation)


```

### 3. Unknown MCP Tool


```bash
curl -X POST "http://localhost:5037/mcp/call_tool" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "unknown_tool",
    "arguments": {}
  }'

# Expected: Tool not found error


```

## CORS Testing

### 1. Preflight Request


```bash
curl -X OPTIONS "http://localhost:5037/ask" \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type"

```

### 2. Cross-Origin Request


```bash
curl -X POST "http://localhost:5037/ask" \
  -H "Origin: http://localhost:3000" \
  -H "Content-Type: application/json" \
  -d '{"query": "test", "streaming": false}'

```

## Expected Response Formats

### NLWeb Response Structure


```json
{
  "queryId": "string",
  "query": "string",
  "mode": "List|Summarize|Generate",
  "results": [
    {
      "url": "string",
      "name": "string",
      "site": "string",
      "score": 0.95,
      "description": "string",
      "schemaObject": {}
    }
  ],
  "summary": "string (for Summarize mode)",
  "answer": "string (for Generate mode)"
}

```

### MCP Tool Response Structure


```json
{
  "content": [
    {
      "type": "text",
      "text": "Response content"
    }
  ]
}

```

### MCP Error Response Structure


```json
{
  "error": {
    "code": "TOOL_NOT_FOUND",
    "message": "Tool 'unknown_tool' not found"
  }
}

```

## Performance Testing

### 1. Load Testing with Multiple Concurrent Requests


```bash

# Using Apache Bench (if available)

ab -n 100 -c 10 "http://localhost:5037/ask?query=test&streaming=false"

```

### 2. Streaming Performance


```bash

# Test streaming response time

time curl "http://localhost:5037/ask?query=long%20query%20for%20testing&streaming=true"

```

## Notes

- All endpoints support both GET and POST methods where applicable
- Streaming responses use Server-Sent Events format
- JSON responses are properly formatted with appropriate content types
- Error responses include proper HTTP status codes
- OpenAPI documentation is available for API exploration
