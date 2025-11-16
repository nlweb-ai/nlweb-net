# GitHub Actions Workflows

This repository uses GitHub Actions for continuous integration and deployment.

## Workflows

### `.github/workflows/build.yml` - Build and Test

**Triggers:**

- Push to `main` or `develop` branches
- Pull requests targeting `main` or `develop` branches

**Jobs:**

1. **Build** (Matrix: Debug, Release)

   - Restores NuGet packages
   - Builds the entire solution
   - Runs any existing tests
   - Uploads build artifacts (Release only)
1. **Code Quality**

   - Runs code analysis with warnings as errors
   - Validates code formatting with `dotnet format`
1. **Security Scan**

   - Scans for vulnerable NuGet packages
   - Fails the build if vulnerabilities are found
1. **Package Validation** (main branch only)

   - Creates NuGet packages
   - Validates package integrity
   - Uploads package artifacts

## Local Testing

To test the build process locally, run:

```bash

# Restore dependencies

dotnet restore

# Build solution

dotnet build --configuration Release

# Check formatting

dotnet format --verify-no-changes

# Create packages

dotnet pack src/NLWebNet --configuration Release --output ./packages

# Check for vulnerable packages

dotnet list package --vulnerable --include-transitive
```

## Requirements

- .NET 10.0 SDK or later
- The workflow uses Ubuntu runners for better performance and cost efficiency
- Tests are run using the built-in MSTest framework

## Status

The current build status is shown by the badge in the main README.md file.
