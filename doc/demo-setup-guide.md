# NLWebNet Demo Setup Guide

This guide provides step-by-step instructions for configuring and testing the NLWebNet demo application with a real LLM backend to fully exercise the NLWeb protocol.

> **Quick Start**: You can run the demo immediately without AI configuration - it will use mock responses to demonstrate the protocol structure and UI. Follow this guide to integrate real AI services for actual AI-powered responses.

## Table of Contents

- [Enhanced Data Source Features](#enhanced-data-source-features)
- [Prerequisites](#prerequisites)
- [Option 1: Azure OpenAI Setup (Recommended)](#option-1-azure-openai-setup-recommended)
- [Option 2: OpenAI API Setup](#option-2-openai-api-setup)
- [Option 3: Quick Start without AI (Mock Mode)](#option-3-quick-start-without-ai-mock-mode)
- [Step 3: Install AI Service Packages](#step-3-install-ai-service-packages)
- [Step 4: Update Service Registration](#step-4-update-service-registration)
- [Step 5: Run the Demo Application](#step-5-run-the-demo-application)
- [Step 6: Test the NLWeb Protocol](#step-6-test-the-nlweb-protocol)
- [Step 7: Verify Real AI Integration](#step-7-verify-real-ai-integration)
- [Step 8: Advanced Testing Scenarios](#step-8-advanced-testing-scenarios)
- [Expected Outcomes](#expected-outcomes)
- [Production Deployment Notes](#production-deployment-notes)
- [Frequently Asked Questions](#frequently-asked-questions)

## Enhanced Data Source Features

The NLWebNet demo application includes sophisticated data source management with strict isolation and visual indicators:

### 🎯 **Smart Data Source Routing**

The demo uses an **Enhanced Mock Data Backend** that intelligently routes queries to different data sources based on LLM availability and query content:

- **When LLM is configured**:
  - **.NET queries** (containing ".NET", "release", "update", etc.) → **RSS feed data** from Microsoft .NET Blog
  - **General queries** (space, movies, AI, etc.) → **Static science fiction content** with Schema.org markup
- **When LLM is not configured**:
  - **All queries** → **Mock placeholder data** for demonstration

### 🎨 **Visual Data Source Indicators**

The demo UI features prominent Bootstrap cards at the top of results showing:

- **🔵 RSS Feeds**: Live .NET blog content (blue primary theme)
- **🟢 Schema.org Static Data**: Science fiction content (info blue theme)
- **🟡 Mock Data**: Placeholder content (warning yellow theme)

Each indicator shows the count of results from that source and is highlighted when active.

### 🏷️ **Result Source Labels**

Every search result includes color-coded badges showing its data source:

- **RSS badge** (blue): Live RSS feed content
- **Schema.org badge** (info blue): Static structured data
- **Mock badge** (yellow): Placeholder content

### 🧹 **Clean Content Display**

- **HTML Tag Removal**: RSS feed content automatically strips HTML tags for clean display
- **Zero Cross-Contamination**: Robust backend logic ensures no mixing of data sources
- **Science Fiction Theme**: Static content features movies, spacecraft, exoplanets, and futuristic technology

### 💡 **User Guidance**

When AI is configured, the demo shows helpful prompts with examples:

- **".NET Content"**: Try ".NET 9", "ASP.NET updates", "C# features"
- **"Science Fiction"**: Try "space movies", "Mars exploration", "AI stories"

This design helps users understand what content is available and demonstrates clean data source separation for NLWeb protocol implementation.

## Prerequisites

- .NET 9 SDK installed
- Git repository cloned locally
- Access to one of the following LLM services:
  - Azure OpenAI (recommended)
  - OpenAI API
  - Other Microsoft.Extensions.AI compatible services

## Option 1: Azure OpenAI Setup (Recommended)

### Step 1: Get Azure OpenAI Credentials

1. **Create an Azure OpenAI Resource**:

   - Go to [Azure Portal](https://portal.azure.com)
   - Create a new "Azure OpenAI" resource
   - Choose a region that supports GPT-4 (e.g., East US, West Europe)
   - Note the endpoint URL (e.g., `https://your-resource.openai.azure.com/`)
1. **Deploy a Model**:

   - In Azure OpenAI Studio, go to "Deployments"
   - Create a new deployment with:
     - Model: `gpt-4` or `gpt-4-turbo`
     - Deployment name: `gpt-4` (or customize)
   - Note the deployment name
1. **Get API Key**:

   - In Azure Portal, go to your OpenAI resource
   - Navigate to "Keys and Endpoint"
   - Copy one of the API keys

### Step 2: Configure the Demo App

1. **Update Configuration**:
   Open `samples/Demo/appsettings.json` and update the Azure OpenAI section:


   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "<https://your-resource.openai.azure.com/>",
       "DeploymentName": "gpt-4",
       "ApiVersion": "2024-02-01",
       "ApiKey": "your-api-key-here"
     }   }


   ```

   > **Security Note**: Never commit API keys to source control. Consider using:
   >
   > - Environment variables: `export AZUREOPENAI__APIKEY="your-key"`
   > - User secrets: `dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"`
   > - Azure Key Vault for production deployments
1. **Optional: Configure Azure Search** (for enhanced data backend):


   ```json
   {
     "AzureSearch": {
       "ServiceName": "your-search-service",
       "IndexName": "nlweb-index",
       "ApiKey": "your-search-api-key"
     }
   }

   ```

## Option 2: OpenAI API Setup

### Step 1: Get OpenAI API Key

1. **Create OpenAI Account**:

   - Go to [OpenAI Platform](https://platform.openai.com)
   - Create an account and verify your phone number
   - Add billing information (required for API access)
1. **Generate API Key**:

   - Go to "API Keys" section
   - Create a new secret key
   - Copy the key (it won't be shown again)

### Step 2: Configure the Demo App for OpenAI


  ```json
   {
     "OpenAI": {
       "ApiKey": "sk-your-api-key-here",
       "Model": "gpt-4",
       "BaseUrl": "<https://api.openai.com/v1>"
     }
   }

   ```

   > **Security Note**: Never commit API keys to source control. Consider using:
   >
   > - Environment variables: `export OPENAI__APIKEY="sk-your-key"`
   > - User secrets: `dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"`
   > - Secure configuration management for production

## Option 3: Quick Start without AI (Mock Mode)

If you want to test the demo immediately without setting up AI services:

1. **Skip AI Configuration**: The demo works out-of-the-box with mock responses
1. **Run Immediately**: `cd samples/Demo && dotnet run`
1. **Test Protocol**: All endpoints work with template-based responses
1. **Add AI Later**: Follow Options 1 or 2 above when ready for real AI integration

This mode is perfect for:

- Understanding the NLWeb protocol structure
- Testing the UI and streaming functionality
- Development and integration testing
- Demonstrations without API costs

## Step 3: Install AI Service Packages

Since NLWebNet uses `Microsoft.Extensions.AI`, you need to install the appropriate AI provider package for your chosen service.

1. **For Azure OpenAI**, install the Azure OpenAI package:


   ```powershell
   cd samples/Demo
   dotnet add package Microsoft.Extensions.AI.AzureAIInference

   ```

1. **For OpenAI API**, install the OpenAI package:


   ```powershell
   cd samples/Demo
   dotnet add package Microsoft.Extensions.AI.OpenAI

   ```

## Step 4: Update Service Registration

1. **Open `samples/Demo/Program.cs`** and add AI service registration.

   **For Azure OpenAI**, add this after the NLWebNet service registration:


   ```csharp
   // Add Azure OpenAI client
   builder.Services.AddAzureOpenAIClient(builder.Configuration.GetSection("AzureOpenAI"));

   ```

   **For OpenAI API**, add this instead:


   ```csharp
   // Add OpenAI client
   builder.Services.AddOpenAIClient(builder.Configuration.GetSection("OpenAI"));

   ```

   > **Note**: The demo app currently works with mock data. Adding real AI services will enable actual AI-powered responses instead of template-based mock responses.
1. **Verify the complete Program.cs** includes these sections:


   ```csharp
   // Add NLWebNet services
   builder.Services.AddNLWebNet(options =>
   {
       options.DefaultMode = NLWebNet.Models.QueryMode.List;
       options.EnableStreaming = true;
   });

   // Add your chosen AI service (Azure OpenAI or OpenAI)
   builder.Services.AddAzureOpenAIClient(builder.Configuration.GetSection("AzureOpenAI"));
   // OR: builder.Services.AddOpenAIClient(builder.Configuration.GetSection("OpenAI"));

   ```

## Step 5: Run the Demo Application

1. **Start the Application**:


   ```bash
   cd samples/Demo
   dotnet run

   ```

1. **Verify Startup**:

   - Look for startup logs showing successful service registration
   - Check that no errors appear related to AI service configuration
   - Application should start on `http://localhost:5037`

   **Important**: Without AI service configuration, the demo will work with **mock responses**. This is by design - you can test the protocol structure and UI without AI services, but responses will be template-based rather than AI-generated.

## Step 6: Test the NLWeb Protocol

### Basic Query Testing

1. **Navigate to the Demo**:

   - Open `http://localhost:5037/nlweb`
   - You should see the NLWebNet Interactive Demo interface
1. **Test List Mode**:

   - Enter a query: "What is machine learning?"
   - Mode: **List**
   - Click "Submit Query"
   - **Expected**: Search results with relevance scores
1. **Test Summarize Mode**:

   - Enter a query: "Explain cloud computing benefits"
   - Mode: **Summarize**
   - Click "Submit Query"
   - **Expected**: AI-generated summary + supporting search results
1. **Test Generate Mode**:

   - Enter a query: "How do neural networks work?"
   - Mode: **Generate**
   - Click "Submit Query"
   - **Expected**: Full AI-generated response with citations

### Enhanced Data Source Testing

The demo features sophisticated data source management. Test these scenarios to see the intelligent routing:

1. **Test .NET Content Routing**:

   - Enter queries: ".NET 9 features", "ASP.NET updates", "C# release notes"
   - **Expected**: Results show RSS badges and come from devblogs.microsoft.com
   - **Visual**: Top data source card shows "Live RSS Feeds" as active (blue highlight)
   - **Note**: HTML tags are automatically stripped from RSS content for clean display
1. **Test Science Fiction Content Routing**:

   - Enter queries: "space movies", "Mars exploration", "AI stories", "exoplanets"
   - **Expected**: Results show Schema.org badges with sci-fi themed content
   - **Visual**: Top data source card shows "Schema.org Static Data" as active (info blue highlight)
   - **Content**: Movies like Blade Runner 2049, spacecraft like Millennium Falcon, etc.
1. **Test Mock Data Mode** (AI not configured):

   - If no AI is configured, any query returns mock placeholder data
   - **Expected**: Results show Mock badges with placeholder content
   - **Visual**: Top data source card shows "Mock Data" as active (warning yellow highlight)
1. **Verify Data Source Isolation**:

   - Check that .NET queries never show sci-fi content
   - Check that general queries never show RSS feed content
   - Each result card shows the correct color-coded badge (RSS/Schema.org/Mock)
1. **Test User Guidance Prompts**:

   - When AI is configured, see helpful example prompts above the query box
   - Examples show what content is available for .NET vs Science Fiction queries

### Streaming Testing

1. **Go to Streaming Tab**:

   - Click the "Streaming" tab in the demo interface
   - Enter a complex query: "Explain the differences between supervised and unsupervised learning"
   - Click "Start Streaming"
   - **Expected**: Real-time streaming response chunks
1. **Monitor Stream Types**:

   - Look for different chunk types: text, result, summary, error
   - Verify auto-scrolling behavior
   - Test "Stop Stream" and "Clear" functionality

### API Endpoint Testing

1. **Go to API Test Tab**:

   - Click the "API Test" tab
   - Configure a test request:
     - Endpoint: `/ask`
     - Query: "What are the benefits of microservices architecture?"
     - Mode: `generate`
     - Streaming: `true`
1. **Execute API Test**:

   - Click "Send Request"
   - **Expected**: JSON response with proper NLWeb protocol structure:


     ```json
     {
       "query_id": "auto-generated-uuid",
       "query": "What are the benefits of microservices architecture?",
       "mode": "generate",
       "generated_response": "AI-generated response...",
       "summary": "Brief summary...",
       "results": [
         {
           "url": "https://example.com/article",
           "name": "Article Title",
           "site": "example.com",
           "score": 0.95,
           "description": "AI-generated description..."
         }
       ]
     }

     ```

### MCP (Model Context Protocol) Testing

1. **Test MCP Tools**:

   - Navigate to "API Test" tab
   - Select endpoint: `/mcp`
   - Test `list_tools` method:


     ```json
     {
       "method": "list_tools"
     }

     ```

   - **Expected**: List of available tools (`nlweb_search`, `nlweb_query_history`)
1. **Test Tool Calling**:

   - Test `call_tool` method:


     ```json
     {
       "method": "call_tool",
       "params": {
         "name": "nlweb_search",
         "arguments": {
           "query": "artificial intelligence trends",
           "mode": "summarize"
         }
       }
     }

     ```

   - **Expected**: Tool execution results in MCP format

## Step 7: Verify Real AI Integration

### Success Indicators

1. **AI-Generated Content**:

   - Responses should be contextual and relevant (not template-based)
   - Content should vary between queries
   - Summaries should be coherent and well-structured
1. **Streaming Behavior**:

   - Real-time token streaming (not pre-built chunks)
   - Progressive response building
   - Natural language flow
1. **Query Processing**:

   - Decontextualization working with previous queries
   - Context-aware responses when using conversation history
   - Proper handling of different query modes

### Troubleshooting

**Common Issues**:

1. **"AI service not configured" errors**:

   - Verify API keys are correct in `appsettings.json`
   - Check endpoint URLs (especially for Azure OpenAI)
   - Ensure AI service packages are installed (`Microsoft.Extensions.AI.AzureAIInference` or `Microsoft.Extensions.AI.OpenAI`)
   - Verify service registration is added to `Program.cs`
1. **Mock responses instead of AI responses**:

   - This is expected behavior if AI services aren't configured
   - Install the appropriate AI package (Step 3)
   - Add service registration to Program.cs (Step 4)
   - Check logs for AI service initialization
   - Verify configuration section names match (`AzureOpenAI` or `OpenAI`)
1. **Package installation errors**:

   - Ensure you're using .NET 9 SDK
   - Try clearing NuGet cache: `dotnet nuget locals all --clear`
   - Verify internet connectivity for package downloads
1. **Streaming not working**:

   - Check browser developer tools for SSE connection
   - Verify Content-Type headers   - Test with different browsers
   - Ensure AI service supports streaming (most do)
1. **Configuration section errors**:

   - Verify JSON structure in `appsettings.json`
   - Check for trailing commas or syntax errors
   - Ensure section names match exactly (`AzureOpenAI` vs `OpenAI`)
1. **Model-specific issues**:

   - Verify your deployed model supports chat completions
   - Some models may have different response formats
   - Check model availability in your region (Azure OpenAI)
   - Ensure sufficient quota/rate limits for your API keys

**Debug Logging**:

Enable detailed logging in `appsettings.json`:


```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NLWebNet": "Debug",
      "Microsoft.Extensions.AI": "Debug"
    }
  }
}

```

## Step 8: Advanced Testing Scenarios

### Alternative Model Configurations

You can customize the AI models used by updating your configuration:

**Azure OpenAI Alternative Models**:


```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4o",  // or gpt-35-turbo, gpt-4-turbo
    "ApiVersion": "2024-02-01",
    "ApiKey": "your-api-key-here"
  }
}

```

**OpenAI Alternative Models**:


```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-4o",  // or gpt-3.5-turbo, gpt-4-turbo
    "BaseUrl": "https://api.openai.com/v1"
  }
}

```

**Custom Configuration Options**:


```json
{
  "NLWebNet": {
    "DefaultMode": "Generate",           // List, Summarize, Generate
    "EnableStreaming": true,
    "DefaultTimeoutSeconds": 60,         // Increase for complex queries
    "MaxResultsPerQuery": 20            // Adjust result count
  }
}

```

### Context and Conversation Testing

1. **Multi-turn Conversations**:

   - Enter initial query: "What is Docker?"
   - Add to "Previous Queries": "What is Docker?"
   - Enter follow-up: "How is it different from virtual machines?"
   - **Expected**: Context-aware response referencing previous query
1. **Site Filtering**:

   - Use "Site" field to filter results: "microsoft.com"
   - **Expected**: Results should be filtered to specified domain (if backend supports it)

### Performance Testing

1. **Load Testing**:

   - Submit multiple simultaneous queries
   - Test streaming with long responses
   - Verify timeout handling (default 30 seconds)
1. **Error Handling**:

   - Submit invalid queries
   - Test with malformed JSON (in API Test tab)
   - Verify graceful error responses

## Expected Outcomes

After completing this setup, you should have:

✅ **Fully Functional NLWeb Implementation**:

- Real AI-powered responses for all query modes
- Working streaming implementation
- Complete MCP integration
- Proper error handling and validation

✅ **Protocol Compliance**:

- JSON responses matching NLWeb specification
- All required fields populated correctly
- Proper HTTP status codes and headers

✅ **Production-Ready Demo**:

- Configurable AI backends
- Comprehensive testing interface
- Real-time streaming capabilities
- MCP tool calling functionality

This setup demonstrates the complete NLWeb protocol implementation with real AI integration, providing a solid foundation for further development and integration testing.

## Production Deployment Notes

When deploying the demo to production environments:

### Security

- Use Azure Key Vault, AWS Secrets Manager, or similar for API keys
- Enable HTTPS with valid certificates
- Configure CORS appropriately for your domain
- Review and harden rate limiting settings

### Monitoring

- Enable detailed logging for troubleshooting
- Set up health checks for your monitoring system
- Configure OpenTelemetry exports to your observability platform
- Monitor AI service usage and costs

### Performance

- Consider connection pooling for high-traffic scenarios
- Configure appropriate timeout values
- Set up load balancing if needed
- Test with realistic query volumes

### Cost Management

- Monitor AI service usage and billing
- Implement usage quotas per user/application
- Consider caching strategies for repeated queries
- Use appropriate model sizes for your use case

### Documentation

- Document your specific configuration choices
- Create runbooks for common operational tasks
- Set up alerting for service health and cost thresholds

## Frequently Asked Questions

### Q: Can I use other AI providers besides Azure OpenAI and OpenAI

A: Yes! NLWebNet uses Microsoft.Extensions.AI, which supports multiple providers. You can add packages like `Microsoft.Extensions.AI.Anthropic` or implement custom `IChatClient` providers for other services.

### Q: Why am I getting template responses instead of AI responses

A: This means the AI services aren't properly configured. Check:

1. AI service packages are installed
1. Service registration is added to Program.cs
1. Configuration keys are correct
1. API keys are valid and have sufficient quota

### Q: Can I run this in a container

A: Yes! The demo includes container support. Use `dotnet publish` with container tools or the included Dockerfile. Remember to handle secrets securely in containerized environments.

### Q: How do I customize the search backend

A: Implement the `IDataBackend` interface and register it in DI. The current implementation uses a mock backend for demonstration purposes.

### Q: What's the difference between the three query modes

A:

- **List**: Returns search results only (no AI processing)
- **Summarize**: AI generates a summary plus supporting search results
- **Generate**: AI generates a full response with minimal search context

### Q: How do I add custom rate limiting

A: Implement `IRateLimitingService` or configure the included `InMemoryRateLimitingService` through NLWebOptions.

### Q: Is this production-ready

A: This is an alpha release for evaluation. For production use, review security, monitoring, error handling, and performance characteristics for your specific requirements.
