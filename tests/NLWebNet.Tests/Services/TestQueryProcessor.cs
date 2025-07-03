using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

/// <summary>
/// Test implementation of IQueryProcessor for unit testing.
/// </summary>
public class TestQueryProcessor : IQueryProcessor
{
    public string? ProcessedQuery { get; set; }
    public List<NLWebRequest> ReceivedRequests { get; } = new();

    public Task<string> ProcessQueryAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add(request);
        return Task.FromResult(ProcessedQuery ?? request.Query);
    }

    public string GenerateQueryId(NLWebRequest request)
    {
        return request.QueryId ?? Guid.NewGuid().ToString();
    }

    public bool ValidateRequest(NLWebRequest request)
    {
        return !string.IsNullOrWhiteSpace(request?.Query);
    }
}
