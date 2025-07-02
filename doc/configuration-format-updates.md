# Configuration Format Updates

This document describes the new configuration format support added to NLWebNet in response to the NLWeb June 2025 release requirements.

## Overview

NLWebNet now supports multiple configuration formats while maintaining full backward compatibility:

- **YAML Configuration**: Enhanced multi-backend configuration with `enabled` flags
- **XML Tool Definitions**: Structured tool definitions for the tool selection framework
- **JSON Configuration**: Existing JSON configuration format continues to work unchanged

## YAML Configuration Support

### Basic Usage

To enable YAML configuration support in your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add YAML configuration format support
builder.Configuration.AddNLWebConfigurationFormats(builder.Environment);

// Register configuration format services
builder.Services.AddNLWebConfigurationFormats(builder.Configuration);
```

### YAML Configuration Format

The new YAML format supports the multi-backend configuration structure introduced in June 2025:

```yaml
# config_retrieval.yaml
write_endpoint: primary_backend
endpoints:
  primary_backend:
    enabled: true
    db_type: azure_ai_search
    priority: 1
    properties:
      service_name: "nlweb-search"
      index_name: "nlweb-index"
      api_version: "2023-11-01"

  secondary_backend:
    enabled: true
    db_type: mock
    priority: 0
    properties:
      data_source: "sample_data"
      enable_fuzzy_matching: true

  backup_backend:
    enabled: false
    db_type: azure_ai_search
    priority: -1
    properties:
      service_name: "nlweb-backup"
      index_name: "nlweb-backup-index"

# Multi-backend settings
enable_parallel_querying: true
enable_result_deduplication: true
max_concurrent_queries: 3
backend_timeout_seconds: 30

# General NLWeb settings
nlweb:
  default_mode: "List"
  enable_streaming: true
  default_timeout_seconds: 30
  max_results_per_query: 50
  enable_caching: true
  cache_expiration_minutes: 60
  tool_selection_enabled: true
```

### Auto-Detection

The configuration system automatically looks for these YAML files:

- `config_retrieval.yaml` / `config_retrieval.yml`
- `nlweb.yaml` / `nlweb.yml`
- Environment-specific versions (e.g., `config_retrieval.Development.yaml`)

### Manual YAML Loading

You can also manually load YAML configuration:

```csharp
builder.Configuration.AddYamlFile("my-config.yaml", optional: true, reloadOnChange: true);

// Or from a stream
using var stream = File.OpenRead("config.yaml");
builder.Configuration.AddYamlStream(stream);
```

## XML Tool Definitions

### Tool Definition Loader Service

The `IToolDefinitionLoader` service provides XML-based tool definition support:

```csharp
// Register the service (included in AddNLWebConfigurationFormats)
builder.Services.AddSingleton<IToolDefinitionLoader, ToolDefinitionLoader>();

// Use the service
var toolLoader = serviceProvider.GetRequiredService<IToolDefinitionLoader>();
var toolDefinitions = await toolLoader.LoadFromFileAsync("tool_definitions.xml");
```

### XML Tool Definition Format

```xml
<?xml version="1.0" encoding="utf-8"?>
<ToolDefinitions>
  <Tool id="enhanced-search" name="Enhanced Search Tool" type="search" enabled="true" priority="10">
    <Description>Advanced search capability with semantic understanding</Description>
    <Parameters>
      <MaxResults>50</MaxResults>
      <TimeoutSeconds>30</TimeoutSeconds>
      <EnableCaching>true</EnableCaching>
      <CustomProperties>
        <Property name="semantic_search" value="true" type="boolean" />
        <Property name="relevance_threshold" value="0.7" type="float" />
      </CustomProperties>
    </Parameters>
    <TriggerPatterns>
      <Pattern>search for*</Pattern>
      <Pattern>find*</Pattern>
      <Pattern>what is*</Pattern>
    </TriggerPatterns>
    <SupportedBackends>
      <Backend>azure_ai_search</Backend>
      <Backend>mock</Backend>
    </SupportedBackends>
  </Tool>
</ToolDefinitions>
```

### Tool Types

Supported tool types:
- `search` - Enhanced search capabilities
- `details` - Retrieve specific information about items
- `compare` - Side-by-side comparison of items
- `ensemble` - Create cohesive sets of related items

## Backward Compatibility

### Existing JSON Configuration

All existing JSON configuration continues to work unchanged:

```json
{
  "NLWebNet": {
    "DefaultMode": "List",
    "EnableStreaming": true,
    "MultiBackend": {
      "Enabled": true,
      "WriteEndpoint": "primary",
      "Endpoints": {
        "primary": {
          "Enabled": true,
          "BackendType": "azure_ai_search"
        }
      }
    }
  }
}
```

### Migration Path

1. **Phase 1**: Add YAML support alongside existing JSON
2. **Phase 2**: Gradually migrate configuration to YAML format
3. **Phase 3**: Optionally remove JSON configuration files (JSON support remains)

No code changes are required to existing applications - the new formats are additive.

## Configuration Options

### ConfigurationFormatOptions

Control configuration format behavior:

```csharp
builder.Services.Configure<NLWebOptions>(options =>
{
    options.ConfigurationFormat.EnableYamlSupport = true;
    options.ConfigurationFormat.EnableXmlToolDefinitions = true;
    options.ConfigurationFormat.AutoDetectConfigurationFiles = true;
    options.ConfigurationFormat.YamlConfigPath = "custom-config.yaml";
    options.ConfigurationFormat.XmlToolDefinitionsPath = "tools.xml";
});
```

## Examples

See the demo application for complete examples:
- `/samples/Demo/config_retrieval.yaml` - Multi-backend YAML configuration
- `/samples/Demo/tool_definitions.xml` - XML tool definitions
- `/samples/Demo/Program.cs` - Integration example

## Dependencies

The new configuration format support adds these dependencies:
- `YamlDotNet` (16.2.1) - YAML parsing
- `Microsoft.Extensions.Configuration.Xml` (9.0.6) - XML configuration support

## Testing

Comprehensive tests cover:
- YAML parsing and configuration binding
- XML tool definition loading and validation
- Service registration and dependency injection
- Backward compatibility with existing JSON configuration
- Error handling and validation

Run tests with: `dotnet test --filter "ConfigurationFormatSupportTests"`