using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLWebNet.Models;
using System.Collections.Concurrent;

namespace NLWebNet.Services;

/// <summary>
/// Implementation of backend manager that coordinates operations across multiple data backends.
/// </summary>
public class BackendManager : IBackendManager
{
    private readonly IEnumerable<IDataBackend> _backends;
    private readonly MultiBackendOptions _options;
    private readonly ILogger<BackendManager> _logger;
    private readonly Dictionary<string, IDataBackend> _backendsByName;
    private readonly IDataBackend? _writeBackend;

    public BackendManager(
        IEnumerable<IDataBackend> backends,
        IOptions<MultiBackendOptions> options,
        ILogger<BackendManager> logger)
    {
        _backends = backends ?? throw new ArgumentNullException(nameof(backends));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // For this initial implementation, we'll work with the backends provided via DI
        // In a full implementation, this would use a factory pattern to create backends
        // based on the configuration
        _backendsByName = new Dictionary<string, IDataBackend>();

        // Assign backends names based on configured endpoints if available,
        // otherwise fall back to generic names for backward compatibility
        var backendArray = _backends.ToArray();
        var configuredEndpoints = _options.Endpoints?.Where(e => e.Value.Enabled).ToList() ?? new List<KeyValuePair<string, BackendEndpointOptions>>();

        if (configuredEndpoints.Count > 0 && configuredEndpoints.Count == backendArray.Length)
        {
            // Use configured endpoint identifiers
            for (int i = 0; i < backendArray.Length; i++)
            {
                var endpointName = configuredEndpoints[i].Key;
                _backendsByName[endpointName] = backendArray[i];
            }
        }
        else
        {
            // Fall back to generic names for backward compatibility
            for (int i = 0; i < backendArray.Length; i++)
            {
                _backendsByName[$"backend_{i}"] = backendArray[i];
            }
        }

        // Set write backend - for now use the first backend if writeEndpoint is configured
        if (!string.IsNullOrEmpty(_options.WriteEndpoint) &&
            _backendsByName.TryGetValue(_options.WriteEndpoint, out var writeBackend))
        {
            _writeBackend = writeBackend;
        }
        else if (backendArray.Length > 0)
        {
            _writeBackend = backendArray[0]; // Default to first backend
        }

        _logger.LogInformation("BackendManager initialized with {BackendCount} backends, WriteEndpoint: {WriteEndpoint}",
            _backendsByName.Count, _options.WriteEndpoint ?? "default");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_backends.Any())
        {
            // Fall back to single backend behavior if multi-backend is disabled
            var firstBackend = _backends.FirstOrDefault();
            if (firstBackend == null)
            {
                return Enumerable.Empty<NLWebResult>();
            }
            return await firstBackend.SearchAsync(query, site, maxResults, cancellationToken);
        }

        _logger.LogDebug("Starting parallel search across {BackendCount} backends for query: {Query}",
            _backends.Count(), query);

        var results = new ConcurrentBag<NLWebResult>();
        var semaphore = new SemaphoreSlim(_options.MaxConcurrentQueries, _options.MaxConcurrentQueries);

        if (_options.EnableParallelQuerying)
        {
            // Execute searches in parallel
            var tasks = _backends.Select(async backend =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.BackendTimeoutSeconds));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var backendResults = await backend.SearchAsync(query, site, maxResults, combinedCts.Token);
                    foreach (var result in backendResults)
                    {
                        results.Add(result);
                    }
                    _logger.LogDebug("Backend search completed with {ResultCount} results", backendResults.Count());
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Backend search timed out or was cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during backend search");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        else
        {
            // Execute searches sequentially
            foreach (var backend in _backends)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.BackendTimeoutSeconds));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var backendResults = await backend.SearchAsync(query, site, maxResults, combinedCts.Token);
                    foreach (var result in backendResults)
                    {
                        results.Add(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Backend search timed out or was cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during backend search");
                }
            }
        }

        var allResults = results.ToList();
        _logger.LogDebug("Collected {TotalResults} results from all backends", allResults.Count);

        // Apply deduplication if enabled
        if (_options.EnableResultDeduplication)
        {
            allResults = DeduplicateResults(allResults);
            _logger.LogDebug("After deduplication: {DeduplicatedResults} results", allResults.Count);
        }

        // Sort by relevance score and take the requested number of results
        return allResults
            .OrderByDescending(r => r.Score)
            .Take(maxResults);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default)
    {
        var allSites = new HashSet<string>();

        foreach (var backend in _backends)
        {
            try
            {
                var sites = await backend.GetAvailableSitesAsync(cancellationToken);
                foreach (var site in sites)
                {
                    allSites.Add(site);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available sites from backend");
            }
        }

        return allSites;
    }

    /// <inheritdoc />
    public async Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        foreach (var backend in _backends)
        {
            try
            {
                var result = await backend.GetItemByUrlAsync(url, cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item by URL from backend");
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IDataBackend? GetWriteBackend()
    {
        return _writeBackend;
    }

    /// <inheritdoc />
    public IEnumerable<BackendInfo> GetBackendInfo()
    {
        return _backendsByName.Select(kvp => new BackendInfo
        {
            Id = kvp.Key,
            Enabled = true, // All registered backends are considered enabled
            IsWriteEndpoint = kvp.Value == _writeBackend,
            Capabilities = kvp.Value.GetCapabilities(),
            Priority = 0 // Default priority, would be configurable in full implementation
        });
    }

    /// <summary>
    /// Deduplicates results based on URL and title similarity.
    /// </summary>
    private List<NLWebResult> DeduplicateResults(List<NLWebResult> results)
    {
        // Use Dictionary for O(n) performance instead of O(n²)
        var resultsByUrl = new Dictionary<string, NLWebResult>();

        foreach (var result in results)
        {
            // Check if we've seen this URL before
            if (resultsByUrl.TryGetValue(result.Url, out var existing))
            {
                // Keep the result with the higher score
                if (result.Score > existing.Score)
                {
                    resultsByUrl[result.Url] = result;
                }
            }
            else
            {
                // First time seeing this URL
                resultsByUrl[result.Url] = result;
            }
        }

        return resultsByUrl.Values.ToList();
    }
}