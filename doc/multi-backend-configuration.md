# Multi-Backend Configuration Example

This example demonstrates how to configure and use the multi-backend retrieval architecture in NLWebNet.

## Single Backend (Default/Legacy)


```csharp
// Traditional single backend setup - still works
services.AddNLWebNet<MockDataBackend>(options =>
{
    options.DefaultMode = QueryMode.List;
    options.MaxResultsPerQuery = 20;
});

```

## Multi-Backend Configuration


```csharp
// New multi-backend setup
services.AddNLWebNetMultiBackend(
    options =>
    {
        options.DefaultMode = QueryMode.List;
        options.MaxResultsPerQuery = 50;
    },
    multiBackendOptions =>
    {
        multiBackendOptions.Enabled = true;
        multiBackendOptions.EnableParallelQuerying = true;
        multiBackendOptions.EnableResultDeduplication = true;
        multiBackendOptions.MaxConcurrentQueries = 3;
        multiBackendOptions.BackendTimeoutSeconds = 30;
        multiBackendOptions.WriteEndpoint = "primary_backend";
    });

```

## Configuration via appsettings.json


```json
{
  "NLWebNet": {
    "DefaultMode": "List",
    "MaxResultsPerQuery": 50,
    "MultiBackend": {
      "Enabled": true,
      "EnableParallelQuerying": true,
      "EnableResultDeduplication": true,
      "MaxConcurrentQueries": 3,
      "BackendTimeoutSeconds": 30,
      "WriteEndpoint": "primary_backend",
      "Endpoints": {
        "primary_backend": {
          "Enabled": true,
          "BackendType": "azure_ai_search",
          "Priority": 10,
          "Properties": {
            "ConnectionString": "your-connection-string",
            "IndexName": "your-index"
          }
        },
        "secondary_backend": {
          "Enabled": true,
          "BackendType": "mock",
          "Priority": 5,
          "Properties": {}
        }
      }
    }
  }
}

```

## Usage Example


```csharp
public class ExampleController : ControllerBase
{
    private readonly INLWebService _nlWebService;
    private readonly IBackendManager _backendManager;

    public ExampleController(INLWebService nlWebService, IBackendManager backendManager)
    {
        _nlWebService = nlWebService;
        _backendManager = backendManager;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] NLWebRequest request)
    {
        // Multi-backend search automatically handled
        var response = await _nlWebService.ProcessRequestAsync(request);
        return Ok(response);
    }

    [HttpGet("backend-info")]
    public IActionResult GetBackendInfo()
    {
        // Get information about configured backends
        var backendInfo = _backendManager.GetBackendInfo();
        return Ok(backendInfo);
    }

    [HttpGet("write-backend-capabilities")]
    public IActionResult GetWriteBackendCapabilities()
    {
        // Access the designated write backend
        var writeBackend = _backendManager.GetWriteBackend();
        if (writeBackend == null)
        {
            return NotFound("No write backend configured");
        }

        var capabilities = writeBackend.GetCapabilities();
        return Ok(capabilities);
    }
}

```

## Key Features

### Parallel Querying

- Queries execute simultaneously across all enabled backends
- Configurable concurrency limits and timeouts
- Graceful handling of backend failures

### Result Deduplication

- Automatic deduplication based on URL
- Higher scoring results from different backends take precedence
- Can be disabled for scenarios requiring all results

### Write Endpoint

- Designate one backend as the primary write endpoint
- Other backends remain read-only for queries
- Useful for hybrid architectures

### Backward Compatibility

- Existing single-backend configurations continue to work
- No breaking changes to existing APIs
- Gradual migration path available

## Migration from Single Backend

1. Replace `AddNLWebNet<T>()` with `AddNLWebNetMultiBackend()`
1. Set `MultiBackend.Enabled = false` initially to maintain existing behavior
1. Configure additional backends in the `Endpoints` section
1. Enable multi-backend mode by setting `MultiBackend.Enabled = true`
1. Test and adjust concurrency and timeout settings as needed
