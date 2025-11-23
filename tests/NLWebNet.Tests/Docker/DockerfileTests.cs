using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace NLWebNet.Tests.Docker;

[TestClass]
public class DockerfileTests
{
    [TestMethod]
    public void Dockerfile_AllReferencedProjectFilesExist()
    {
        // Arrange
        var rootPath = GetRepositoryRoot();
        var dockerfilePath = Path.Combine(rootPath, "deployment/docker/Dockerfile");

        Assert.IsTrue(File.Exists(dockerfilePath), $"Dockerfile not found at {dockerfilePath}");

        var dockerfileContent = File.ReadAllText(dockerfilePath);

        // Extract COPY commands that reference .csproj files
        var copyPattern = @"COPY \[""([^""]+\.csproj)"", ""[^""]+/""\]";
        var matches = Regex.Matches(dockerfileContent, copyPattern);

        // Act & Assert
        foreach (Match match in matches)
        {
            var projectPath = match.Groups[1].Value;
            var fullPath = Path.Combine(rootPath, projectPath);

            Assert.IsTrue(File.Exists(fullPath),
                $"Project file referenced in Dockerfile does not exist: {projectPath} (Full path: {fullPath})");
        }

        // Verify we found at least the expected project files
        Assert.IsGreaterThanOrEqualTo(3, matches.Count,
            "Expected to find at least 3 project file references in Dockerfile (NLWebNet, Demo, Tests)");
    }

    [TestMethod]
    public void Dockerfile_DoesNotReferenceAspireHostProject()
    {
        // Arrange
        var rootPath = GetRepositoryRoot();
        var dockerfilePath = Path.Combine(rootPath, "deployment/docker/Dockerfile");

        Assert.IsTrue(File.Exists(dockerfilePath), $"Dockerfile not found at {dockerfilePath}");

        var dockerfileContent = File.ReadAllText(dockerfilePath);

        // Act & Assert
        Assert.DoesNotContain("AspireHost", dockerfileContent,
            "Dockerfile should not contain references to AspireHost project");
        Assert.DoesNotContain("NLWebNet.AspireHost.csproj", dockerfileContent,
            "Dockerfile should not contain references to NLWebNet.AspireHost.csproj");
    }

    private static string GetRepositoryRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "NLWebNet.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Could not find repository root (NLWebNet.sln not found)");
        return directory.FullName;
    }
}