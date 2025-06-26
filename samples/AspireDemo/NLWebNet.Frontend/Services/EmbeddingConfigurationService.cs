using Microsoft.JSInterop;

namespace NLWebNet.Frontend.Services;

public interface IEmbeddingConfigurationService
{
    bool IsConfigured { get; }
    string? GetGitHubToken();
    Task<bool> ConfigureGitHubTokenAsync(string token);
    Task ClearConfigurationAsync();
    Task InitializeAsync();
    event EventHandler<bool>? ConfigurationChanged;
}

public class EmbeddingConfigurationService : IEmbeddingConfigurationService
{
    private string? _githubToken;
    private readonly ILogger<EmbeddingConfigurationService> _logger;
    private readonly IJSRuntime _jsRuntime;
    private bool _initialized = false;

    public EmbeddingConfigurationService(ILogger<EmbeddingConfigurationService> logger, IJSRuntime jsRuntime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_githubToken);

    public string? GetGitHubToken() => _githubToken;

    public event EventHandler<bool>? ConfigurationChanged;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            // Try to restore token from session storage
            var storedToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "github-token");
            
            if (!string.IsNullOrEmpty(storedToken))
            {
                _githubToken = storedToken;
                _logger.LogInformation("GitHub token restored from session storage");
                ConfigurationChanged?.Invoke(this, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore token from session storage");
            // Session storage not available or error - continue without token
        }
        
        _initialized = true;
    }

    public async Task<bool> ConfigureGitHubTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Empty token provided for GitHub Models configuration");
                return false;
            }

            _githubToken = token;
            
            // Store in session storage
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "github-token", token);
            
            _logger.LogInformation("GitHub Models token configured and stored in session storage");
            
            ConfigurationChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring GitHub Models token");
            return false;
        }
    }

    public async Task ClearConfigurationAsync()
    {
        try
        {
            _githubToken = null;
            
            // Remove from session storage
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "github-token");
            
            _logger.LogInformation("GitHub Models configuration cleared from session storage");
            
            ConfigurationChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing GitHub Models configuration");
            throw;
        }
    }
}
