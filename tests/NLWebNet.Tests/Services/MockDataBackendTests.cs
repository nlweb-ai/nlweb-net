using NLWebNet.Models;
using NLWebNet.Services;

namespace NLWebNet.Tests.Services;

[TestClass]
public class MockDataBackendTests
{
    private MockDataBackend _mockDataBackend = null!;

    [TestInitialize]
    public void Initialize()
    {
        var logger = new TestLogger<MockDataBackend>();
        _mockDataBackend = new MockDataBackend(logger);
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsResults()
    {
        // Arrange
        var query = "NET Core"; // Use a term that appears in the sample data

        // Act
        var results = await _mockDataBackend.SearchAsync(query, null, 10, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsGreaterThan(0, resultsList.Count);
        foreach (var result in resultsList)
        {
            Assert.IsNotNull(result.Name);
            Assert.IsGreaterThan(0, result.Name.Length);
            Assert.IsNotNull(result.Url);
            Assert.IsGreaterThan(0, result.Url.Length);
            Assert.IsNotNull(result.Description);
            Assert.IsGreaterThan(0, result.Description.Length);
        }
    }
    [TestMethod]
    public async Task SearchAsync_WithSiteFilter_ReturnsFilteredResults()
    {
        // Arrange
        var query = "young"; // Use a term that appears in the sci-fi cinema data descriptions
        var site = "scifi-cinema.com"; // Use a site that exists in the sci-fi sample data

        // Act
        var results = await _mockDataBackend.SearchAsync(query, site, 10, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsGreaterThan(0, resultsList.Count, "Should find results for 'young' content on scifi-cinema.com");
        foreach (var result in resultsList)
        {
            Assert.AreEqual(site, result.Site, $"All results should be from site: {site}");
        }
    }
    [TestMethod]
    public async Task SearchAsync_RespectsMaxResults()
    {
        // Arrange
        var query = "space"; // Use a term that will match multiple results in sci-fi data
        var maxResults = 3;

        // Act
        var results = await _mockDataBackend.SearchAsync(query, null, maxResults, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsLessThanOrEqualTo(maxResults, resultsList.Count, $"Should return at most {maxResults} results, got {resultsList.Count}");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults(string? query)
    {
        // Act
        var results = await _mockDataBackend.SearchAsync(query!, null, 10, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsEmpty(resultsList);
    }
}
