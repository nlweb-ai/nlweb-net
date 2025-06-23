using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NLWebNet.Demo.Services;

/// <summary>
/// Simple GitHub Models chat client implementation
/// </summary>
public class GitHubModelsChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public GitHubModelsChatClient(HttpClient httpClient, string model)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }
    public ChatClientMetadata Metadata => new("GitHub Models");

    public object? GetService(Type serviceType, object? serviceKey = null) => null; public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(chatMessages, options, streaming: false);
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        try
        {
            // Log diagnostics
            System.Diagnostics.Debug.WriteLine($"[GitHubModelsChatClient] POST v1/chat/completions");
            System.Diagnostics.Debug.WriteLine($"BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"Authorization: {_httpClient.DefaultRequestHeaders.Authorization}");
            System.Diagnostics.Debug.WriteLine($"Request JSON: {requestJson}");

            var response = await _httpClient.PostAsync("v1/chat/completions", content, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"Response content: {responseContent}");
            var chatResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, JsonOptions);

            if (chatResponse?.Choices?.FirstOrDefault()?.Message is { } message)
            {
                return new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, message.Content) });
            }

            throw new InvalidOperationException("No response from GitHub Models");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GitHubModelsChatClient] Exception: {ex}");
            throw;
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // For simplicity, just return the non-streaming response as a single update
        return GetStreamingResponseAsyncCore(chatMessages, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsyncCore(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completion = await GetResponseAsync(chatMessages, options, cancellationToken);
        var message = completion.Messages.FirstOrDefault();
        if (message != null)
        {
            yield return new ChatResponseUpdate
            {
                Role = message.Role,
                Contents = message.Contents.ToList()
            };
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool streaming)
    {
        return new
        {
            model = _model,
            messages = chatMessages.Select(m => new
            {
                role = m.Role.Value.ToLowerInvariant(),
                content = m.Text ?? string.Empty
            }).ToArray(),
            max_tokens = options?.MaxOutputTokens ?? 1000,
            temperature = options?.Temperature ?? 0.7,
            stream = streaming
        };
    }

    private class OpenAIResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public OpenAIMessage? Message { get; set; }
    }

    private class OpenAIMessage
    {
        public string? Content { get; set; }
    }
}
