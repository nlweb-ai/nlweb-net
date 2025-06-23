using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NLWebNet.Models;

namespace NLWebNet.Demo.Services;

/// <summary>
/// Factory that provides the appropriate IChatClient based on current configuration
/// </summary>
public interface IDynamicChatClientFactory
{
    IChatClient? GetChatClient();
}

public class DynamicChatClientFactory : IDynamicChatClientFactory
{
    private readonly IAIConfigurationService _aiConfigService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DynamicChatClientFactory> _logger;

    public DynamicChatClientFactory(
        IAIConfigurationService aiConfigService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DynamicChatClientFactory> logger)
    {
        _aiConfigService = aiConfigService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }
    public IChatClient? GetChatClient()
    {
        _logger.LogInformation("DynamicChatClientFactory.GetChatClient() called");

        // First check if GitHub Models is configured
        if (_aiConfigService is AIConfigurationService aiService)
        {
            var githubClient = aiService.GetConfiguredChatClient();
            if (githubClient != null)
            {
                _logger.LogInformation("Using configured GitHub Models client");
                return githubClient;
            }
            else
            {
                _logger.LogInformation("GitHub Models client is null");
            }
        }

        // Fall back to any pre-configured IChatClient from DI
        var configuredClient = _serviceProvider.GetService<IChatClient>();
        if (configuredClient != null)
        {
            _logger.LogInformation("Using pre-configured IChatClient from DI");
            return configuredClient;
        }

        // Check if we can create one from configuration
        var azureOpenAIKey = _configuration["AzureOpenAI:ApiKey"];
        var openAIKey = _configuration["OpenAI:ApiKey"];

        if (!string.IsNullOrEmpty(azureOpenAIKey) || !string.IsNullOrEmpty(openAIKey))
        {
            _logger.LogInformation("AI configuration found in appsettings, but no IChatClient configured in DI");
        }
        else
        {
            _logger.LogInformation("No AI provider configured - will use mock responses");
        }

        return null;
    }
}