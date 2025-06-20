using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.MCP;

/// <summary>
/// Test implementation of INLWebService for unit testing.
/// </summary>
public class TestNLWebService : INLWebService
{
    public NLWebResponse? ExpectedResponse { get; set; }
    public List<NLWebRequest> ReceivedRequests { get; } = new();

    public Task<NLWebResponse> ProcessRequestAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add(request);

        return Task.FromResult(ExpectedResponse ?? new NLWebResponse
        {
            QueryId = request.QueryId ?? Guid.NewGuid().ToString(),
            Results = new List<NLWebResult>()
        });
    }

    public IAsyncEnumerable<NLWebResponse> ProcessRequestStreamAsync(NLWebRequest request, CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add(request);
        return ProcessRequestStreamAsyncInternal();
    }

    private async IAsyncEnumerable<NLWebResponse> ProcessRequestStreamAsyncInternal()
    {
        yield return ExpectedResponse ?? new NLWebResponse
        {
            QueryId = Guid.NewGuid().ToString(),
            Results = new List<NLWebResult>()
        };
        await Task.CompletedTask;
    }
}
