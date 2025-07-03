namespace NLWebNet.Tests.TestData;

/// <summary>
/// Constants for test data to avoid magic strings and ensure consistency
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// Tool names used in test scenarios (capitalized versions for test data)
    /// </summary>
    public static class Tools
    {
        public const string Search = "Search";
        public const string Compare = "Compare";
        public const string Details = "Details";
        public const string Ensemble = "Ensemble";
    }

    /// <summary>
    /// Test categories used to group and filter test scenarios
    /// </summary>
    public static class Categories
    {
        public const string BasicSearch = "BasicSearch";
        public const string Technical = "Technical";
        public const string Compare = "Compare";
        public const string Details = "Details";
        public const string Ensemble = "Ensemble";
        public const string EdgeCase = "EdgeCase";
        public const string SiteFiltering = "SiteFiltering";
        public const string EndToEnd = "EndToEnd";
        public const string ToolSelection = "ToolSelection";
        public const string Complex = "Complex";
        public const string Validation = "Validation";
    }
}
