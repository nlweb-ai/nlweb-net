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
        Assert.IsTrue(resultsList.Count > 0);
        foreach (var result in resultsList)
        {
            Assert.IsNotNull(result.Name);
            Assert.IsTrue(result.Name.Length > 0);
            Assert.IsNotNull(result.Url);
            Assert.IsTrue(result.Url.Length > 0);
            Assert.IsNotNull(result.Description);
            Assert.IsTrue(result.Description.Length > 0);
        }
    }

    [TestMethod]
    public async Task SearchAsync_WithSiteFilter_ReturnsFilteredResults()
    {
        // Arrange
        var query = "documentation"; // Use a term that appears in the sample data
        var site = "docs.microsoft.com"; // Use a site that exists in sample data

        // Act
        var results = await _mockDataBackend.SearchAsync(query, site, 10, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsTrue(resultsList.Count > 0);
        foreach (var result in resultsList)
        {
            Assert.AreEqual(site, result.Site);
        }
    }

    [TestMethod]
    public async Task SearchAsync_RespectsMaxResults()
    {
        // Arrange
        var query = "microsoft"; // Use a term that will match multiple results
        var maxResults = 3;

        // Act
        var results = await _mockDataBackend.SearchAsync(query, null, maxResults, CancellationToken.None);
        var resultsList = results.ToList();

        // Assert
        Assert.IsTrue(resultsList.Count <= maxResults);
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
        Assert.AreEqual(0, resultsList.Count);
    }
}
