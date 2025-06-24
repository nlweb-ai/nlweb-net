using Microsoft.Extensions.Logging;
using NLWebNet.Models;
using System.Text.Json;

namespace NLWebNet.Services;

/// <summary>
/// Mock implementation of IDataBackend for testing and demo purposes.
/// Provides sample data and basic search functionality.
/// </summary>
public class MockDataBackend : IDataBackend
{
    private readonly ILogger<MockDataBackend> _logger;
    private readonly List<NLWebResult> _sampleData;

    public MockDataBackend(ILogger<MockDataBackend> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleData = GenerateSampleData();
    }    /// <inheritdoc />
    public async Task<IEnumerable<NLWebResult>> SearchAsync(string query, string? site = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching for query: {Query}, site: {Site}, maxResults: {MaxResults}", query, site, maxResults);

        await Task.Delay(100, cancellationToken); // Simulate network delay

        // Handle null or empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("Empty or null query provided, returning empty results");
            return Enumerable.Empty<NLWebResult>();
        }

        var queryTerms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 2) // Ignore very short terms
            .ToList();

        var results = _sampleData
            .Where(item => site == null || item.Site == site)
            .Select(item => new
            {
                Item = item,
                Score = CalculateRelevanceScore(item, queryTerms)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .Take(Math.Min(maxResults, 50)) // Cap at 50 for demo
            .Select(result => new NLWebResult
            {
                Url = result.Item.Url,
                Name = result.Item.Name,
                Site = result.Item.Site,
                Score = result.Score,
                Description = result.Item.Description,
                SchemaObject = result.Item.SchemaObject
            })
            .ToList();

        _logger.LogDebug("Found {ResultCount} results for query", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableSitesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var sites = _sampleData
            .Select(item => item.Site)
            .Where(site => !string.IsNullOrEmpty(site))
            .Distinct()
            .OrderBy(site => site)
            .ToList();

        return sites!;
    }

    /// <inheritdoc />
    public async Task<NLWebResult?> GetItemByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return _sampleData.FirstOrDefault(item =>
            string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public BackendCapabilities GetCapabilities()
    {
        return new BackendCapabilities
        {
            SupportsSiteFiltering = true,
            SupportsFullTextSearch = true,
            SupportsSemanticSearch = false,
            MaxResults = 50,
            Description = "Mock data backend for testing and demonstration purposes"
        };
    }

    /// <summary>
    /// Calculates a relevance score for an item based on query terms.
    /// </summary>
    private static double CalculateRelevanceScore(NLWebResult item, List<string> queryTerms)
    {
        if (!queryTerms.Any()) return 0;

        var searchableText = $"{item.Name} {item.Description}".ToLowerInvariant();
        var score = 0.0;

        foreach (var term in queryTerms)
        {
            // Exact name match gets highest score
            if (item.Name.ToLowerInvariant().Contains(term))
            {
                score += 10.0;
            }
            // Description match gets medium score
            else if (item.Description?.ToLowerInvariant().Contains(term) == true)
            {
                score += 5.0;
            }
        }

        // Boost score for items that match multiple terms
        var matchingTerms = queryTerms.Count(term => searchableText.Contains(term));
        if (matchingTerms > 1)
        {
            score *= (1.0 + 0.2 * (matchingTerms - 1));
        }

        return Math.Round(score, 2);
    }
    private static List<NLWebResult> GenerateSampleData()
    {
        return new List<NLWebResult>
        {
            // Science Fiction Spacecraft & Technology
            new()
            {
                Url = "https://galactic-shipyards.com/millennium-falcon",
                Name = "Millennium Falcon Technical Specifications",
                Site = "galactic-shipyards.com",
                Score = 0,
                Description = "Complete technical breakdown of the legendary Corellian YT-1300 light freighter, including hyperdrive capabilities and modifications.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Product",
                    name = "Millennium Falcon",
                    brand = "Corellian Engineering Corporation",
                    category = "Light Freighter",
                    specifications = "Modified YT-1300"
                })
            },
            new()
            {
                Url = "https://starfleet-database.com/enterprise-nx01",
                Name = "Enterprise NX-01 Mission Archives",
                Site = "starfleet-database.com",
                Score = 0,
                Description = "Historical records of humanity's first deep space exploration vessel and its groundbreaking missions to establish interstellar relations.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Product",
                    name = "Enterprise NX-01",
                    brand = "Earth Starfleet",
                    category = "Exploration Vessel",
                    serviceType = "Deep Space Exploration"
                })
            },
            new()
            {
                Url = "https://cyberdyne-systems.com/terminator-series",
                Name = "Cyberdyne Systems T-800 Series",
                Site = "cyberdyne-systems.com",
                Score = 0,
                Description = "Advanced cybernetic organism specifications for the T-800 endoskeleton, featuring neural net processors and combat protocols.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Product",
                    name = "T-800 Series Terminator",
                    brand = "Cyberdyne Systems",
                    category = "Cybernetic Organism",
                    model = "Model 101"
                })
            },
            new()
            {
                Url = "https://weyland-yutani.com/nostromo-specs",
                Name = "USCSS Nostromo Commercial Towing Vessel",
                Site = "weyland-yutani.com",
                Score = 0,
                Description = "Heavy-duty commercial towing vehicle designed for long-haul cargo operations across the outer rim territories.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Product",
                    name = "USCSS Nostromo",
                    brand = "Weyland-Yutani",
                    category = "Commercial Towing Vessel",
                    designation = "Commercial Starship Class"
                })
            },

            // Science Fiction Blog Content
            new()
            {
                Url = "https://future-chronicles.com/artificial-intelligence-breakthrough",
                Name = "The Great AI Awakening of 2157",
                Site = "future-chronicles.com",
                Score = 0,
                Description = "A detailed account of humanity's first contact with truly sentient artificial intelligence and the societal changes that followed.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "BlogPosting",
                    headline = "The Great AI Awakening of 2157",
                    author = "Dr. Sarah Chen",
                    datePublished = "2157-03-15",
                    category = "artificial-intelligence"
                })
            },
            new()
            {
                Url = "https://space-exploration-journal.com/mars-colony-update",
                Name = "New Olympia Mars Colony: Year Five Report",
                Site = "space-exploration-journal.com",
                Score = 0,
                Description = "Comprehensive update on the progress of humanity's first permanent settlement on Mars, including terraforming advances and population growth.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "BlogPosting",
                    headline = "New Olympia Mars Colony: Year Five Report",
                    author = "Commander Lisa Rodriguez",
                    datePublished = "2089-07-20",
                    category = "space-colonization"
                })
            },
            new()
            {
                Url = "https://quantum-physics-today.com/faster-than-light-discovery",
                Name = "Breakthrough in Alcubierre Drive Technology",
                Site = "quantum-physics-today.com",
                Score = 0,
                Description = "Revolutionary advances in space-time manipulation bring practical faster-than-light travel closer to reality than ever before.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "BlogPosting",
                    headline = "Breakthrough in Alcubierre Drive Technology",
                    author = "Prof. Miguel Alcubierre Jr.",
                    datePublished = "2145-11-08",
                    category = "space-technology"
                })
            },

            // Science Fiction Movies
            new()
            {
                Url = "https://scifi-cinema.com/movies/blade-runner-2049",
                Name = "Blade Runner 2049",
                Site = "scifi-cinema.com",
                Score = 0,
                Description = "A young blade runner discovers a secret that leads him to track down former blade runner Rick Deckard, missing for thirty years.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Movie",
                    name = "Blade Runner 2049",
                    director = "Denis Villeneuve",
                    datePublished = "2017-10-06",
                    genre = new[] { "Science Fiction", "Drama", "Thriller" },
                    duration = "PT164M",
                    aggregateRating = new { type = "AggregateRating", ratingValue = 8.0, reviewCount = 450000 }
                })
            },
            new()
            {
                Url = "https://scifi-cinema.com/movies/dune-2021",
                Name = "Dune",
                Site = "scifi-cinema.com",
                Score = 0,
                Description = "Feature adaptation of Frank Herbert's science fiction novel about a young man's journey to an alien planet.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Movie",
                    name = "Dune",
                    director = "Denis Villeneuve",
                    datePublished = "2021-10-22",
                    genre = new[] { "Science Fiction", "Adventure", "Drama" },
                    duration = "PT155M",
                    aggregateRating = new { type = "AggregateRating", ratingValue = 8.1, reviewCount = 620000 }
                })
            },
            new()
            {
                Url = "https://scifi-cinema.com/movies/interstellar",
                Name = "Interstellar",
                Site = "scifi-cinema.com",
                Score = 0,
                Description = "When Earth becomes uninhabitable, a team of astronauts travels through a wormhole in search of a new home for humanity.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Movie",
                    name = "Interstellar",
                    director = "Christopher Nolan",
                    datePublished = "2014-11-07",
                    genre = new[] { "Science Fiction", "Drama", "Adventure" },
                    duration = "PT169M",
                    aggregateRating = new { type = "AggregateRating", ratingValue = 8.6, reviewCount = 1700000 }
                })
            },

            // Science Fiction Events
            new()
            {
                Url = "https://galactic-convention.com/worldcon-2157",
                Name = "World Science Fiction Convention 2157",
                Site = "galactic-convention.com",
                Score = 0,
                Description = "The premier gathering of science fiction enthusiasts, featuring panels on space exploration, AI ethics, and first contact protocols.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Event",
                    name = "World Science Fiction Convention 2157",
                    startDate = "2157-08-25",
                    endDate = "2157-08-28",
                    location = "Luna City, Moon",
                    eventAttendanceMode = "MixedEventAttendanceMode",
                    organizer = "World Science Fiction Society"
                })
            },

            // Science Fiction Recipes (futuristic food)
            new()
            {
                Url = "https://space-cuisine.com/recipes/martian-protein-bars",
                Name = "High-Energy Martian Protein Bars",
                Site = "space-cuisine.com",
                Score = 0,
                Description = "Nutrient-dense protein bars designed for long-duration space missions, featuring lab-grown protein and hydroponic ingredients.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Recipe",
                    name = "High-Energy Martian Protein Bars",
                    cookTime = "PT30M",
                    prepTime = "PT15M",
                    totalTime = "PT45M",
                    recipeYield = "12 bars",
                    nutrition = new { calories = "350", protein = "25g", carbohydrateContent = "30g" }
                })
            },

            // Exoplanet Research
            new()
            {
                Url = "https://galactic-library.com/exoplanet-discovery",
                Name = "The Kepler 442b Discovery",
                Site = "galactic-library.com",
                Score = 0,
                Description = "A fascinating account of the discovery of Kepler 442b, an Earth-like exoplanet in the habitable zone of its star.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "Article",
                    name = "The Kepler 442b Discovery",
                    about = "Exoplanet Research",
                    keywords = "astronomy, exoplanets, space exploration"
                })
            },
            new()
            {
                Url = "https://future-archives.org/mars-terraforming",
                Name = "Mars Terraforming Timeline",
                Site = "future-archives.org",
                Score = 0,
                Description = "A comprehensive timeline of theoretical Mars terraforming projects and the technologies required to make the red planet habitable.",
                SchemaObject = JsonSerializer.SerializeToElement(new {
                    type = "TechArticle",
                    name = "Mars Terraforming Timeline",
                    about = "Planetary Engineering",
                    keywords = "mars, terraforming, space colonization"
                })
            }
        };
    }
}
