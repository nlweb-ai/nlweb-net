using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NLWebNet.Models;

namespace NLWebNet.Demo.Services;

public interface IAIConfigurationService
{
    bool IsAIConfigured { get; }
    Task<bool> ConfigureGitHubModelsAsync(string token, string model);
    Task ClearGitHubConfigurationAsync();
    string GetCurrentProvider();
    IChatClient? GetConfiguredChatClient();
}

public class AIConfigurationService : IAIConfigurationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private IChatClient? _gitHubChatClient;
    private bool _isGitHubConfigured = false;

    public AIConfigurationService(
        IServiceProvider serviceProvider,
        ILogger<AIConfigurationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsAIConfigured
    {
        get
        {
            // Check if GitHub Models is configured in session
            if (_isGitHubConfigured && _gitHubChatClient != null)
            {
                return true;
            }

            // Check if any AI provider is configured in appsettings
            var azureOpenAIKey = _configuration["AzureOpenAI:ApiKey"];
            var openAIKey = _configuration["OpenAI:ApiKey"];

            return !string.IsNullOrEmpty(azureOpenAIKey) || !string.IsNullOrEmpty(openAIKey);
        }
    }
    public Task<bool> ConfigureGitHubModelsAsync(string token, string model)
    {
        try
        {
            _logger.LogInformation("Configuring GitHub Models with model: {Model}", model);
            _logger.LogInformation("GitHub token provided: {HasToken}", !string.IsNullOrWhiteSpace(token));

            // Create GitHub Models chat client using a simple approach
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://models.inference.ai.azure.com/");
            _logger.LogInformation("HttpClient BaseAddress set to: {BaseAddress}", httpClient.BaseAddress);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("Authorization header set: {Header}", httpClient.DefaultRequestHeaders.Authorization?.ToString());

            // Create a simple chat client wrapper for GitHub Models
            _gitHubChatClient = new GitHubModelsChatClient(httpClient, model);

            _isGitHubConfigured = true;
            _logger.LogInformation("GitHub Models configured successfully");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring GitHub Models");
            return Task.FromResult(false);
        }
    }

    public Task ClearGitHubConfigurationAsync()
    {
        _gitHubChatClient = null;
        _isGitHubConfigured = false;
        _logger.LogInformation("GitHub Models configuration cleared");
        return Task.CompletedTask;
    }

    public string GetCurrentProvider()
    {
        if (_isGitHubConfigured)
        {
            return "GitHub Models";
        }

        var azureOpenAIKey = _configuration["AzureOpenAI:ApiKey"];
        var openAIKey = _configuration["OpenAI:ApiKey"];

        if (!string.IsNullOrEmpty(azureOpenAIKey))
        {
            return "Azure OpenAI";
        }

        if (!string.IsNullOrEmpty(openAIKey))
        {
            return "OpenAI";
        }

        return "Mock/Demo Mode";
    }

    public IChatClient? GetConfiguredChatClient()
    {
        return _gitHubChatClient;
    }
}
