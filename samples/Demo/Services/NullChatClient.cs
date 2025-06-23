using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace NLWebNet.Demo.Services;

/// <summary>
/// A null implementation of IChatClient that returns empty responses
/// Used when no AI provider is configured
/// </summary>
public class NullChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("Null");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Return an empty response - this will cause ResultGenerator to fall back to mock responses
        return Task.FromResult(new ChatResponse(new List<ChatMessage>()));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Return no streaming updates
        await Task.CompletedTask;
        yield break;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}