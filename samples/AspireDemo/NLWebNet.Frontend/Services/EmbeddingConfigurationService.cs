namespace NLWebNet.Frontend.Services;

public interface IEmbeddingConfigurationService
{
    bool IsConfigured { get; }
    string? GetGitHubToken();
    Task<bool> ConfigureGitHubTokenAsync(string token);
    Task ClearConfigurationAsync();
    event EventHandler<bool>? ConfigurationChanged;
}

public class EmbeddingConfigurationService : IEmbeddingConfigurationService
{
    private string? _githubToken;
    private readonly ILogger<EmbeddingConfigurationService> _logger;

    public EmbeddingConfigurationService(ILogger<EmbeddingConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_githubToken);

    public string? GetGitHubToken() => _githubToken;

    public event EventHandler<bool>? ConfigurationChanged;

    public Task<bool> ConfigureGitHubTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Empty token provided for GitHub Models configuration");
                return Task.FromResult(false);
            }

            _githubToken = token;
            _logger.LogInformation("GitHub Models token configured successfully");
            
            ConfigurationChanged?.Invoke(this, true);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring GitHub Models token");
            return Task.FromResult(false);
        }
    }

    public Task ClearConfigurationAsync()
    {
        try
        {
            _githubToken = null;
            _logger.LogInformation("GitHub Models configuration cleared");
            
            ConfigurationChanged?.Invoke(this, false);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing GitHub Models configuration");
            throw;
        }
    }
}
