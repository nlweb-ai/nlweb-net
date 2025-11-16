# Package Validation Guide

This guide outlines the comprehensive validation process for the NLWebNet NuGet package before publication.

## 🏃‍♂️ Quick Validation

### Windows (PowerShell)


```powershell
.\scripts\validate-package.ps1

```

### Linux/macOS (Bash)


```bash
./deployment/scripts/validate-package.sh

```

## 📋 Manual Validation Checklist

If you prefer to run validation steps manually, here's the complete checklist:

### 1. Build and Test Validation ✅

- [x] Project builds successfully in Release configuration
- [x] All unit tests pass (39/39 tests)
- [x] No compilation warnings or errors
- [x] Demo application runs successfully

### 2. Package Creation ✅

- [x] Package creates successfully with `dotnet pack`
- [x] Both main package (.nupkg) and symbols package (.snupkg) are generated
- [x] Package size is reasonable (typically < 500KB for libraries)

### 3. Package Content Validation

- [ ] **Assembly Content**: Verify NLWebNet.dll contains all expected types
  - `NLWebNet.Models.NLWebRequest`
  - `NLWebNet.Models.NLWebResponse`
  - `NLWebNet.Services.INLWebService`
  - `NLWebNet.Extensions.ServiceCollectionExtensions`
  - `NLWebNet.Extensions.ApplicationBuilderExtensions`
- [ ] **Metadata Files**: Check for README.md, license, and nuspec
- [ ] **Framework Targeting**: Confirm targets .NET 10.0
- [ ] **Dependencies**: Verify all package references are included

### 4. API Surface Validation ✅

- [x] Extension methods are accessible via `using NLWebNet;`
- [x] `AddNLWebNet()` method works with IntelliSense
- [x] `MapNLWebNet()` method works with IntelliSense
- [x] All public APIs are discoverable

### 5. Security and Quality Checks

- [ ] **Vulnerability Scan**: No vulnerable dependencies


  ```bash
  dotnet list package --vulnerable --include-transitive

  ```

- [ ] **Deprecated Dependencies**: Check for deprecated packages


  ```bash
  dotnet list package --deprecated

  ```

- [ ] **Package Validation Tool**: Pass NuGet's validation


  ```bash
  dotnet validate package local [package.nupkg]

  ```

### 6. Integration Testing

- [ ] **Fresh Consumer Project**: Create new project and add package
- [ ] **Compilation Test**: Verify consumer project compiles
- [ ] **Runtime Test**: Confirm basic functionality works
- [ ] **IntelliSense Test**: Verify code completion works

### 7. Metadata Validation

- [ ] **Package Identity**: Correct ID, version, authors
- [ ] **Descriptions**: Meaningful description and tags
- [ ] **URLs**: Valid project and repository URLs
- [ ] **License**: Proper license expression (MIT)
- [ ] **Release Notes**: Clear and informative

### 8. Symbol Package Validation

- [ ] **PDB Files**: Debugging symbols are included
- [ ] **Source Link**: GitHub source linking works
- [ ] **Deterministic Build**: Reproducible builds enabled

## 🚨 Known Acceptable Warnings

These warnings are expected and acceptable for the current release:

### ModelContextProtocol Prerelease Dependency


```text
Warning: Package 'ModelContextProtocol 0.2.0-preview.3' is prerelease

```

**Resolution**: NLWebNet is correctly marked as `1.0.0-beta.1` to indicate prerelease status.

## 🔧 Validation Tools

### Automated Tools Used

- **dotnet-validate**: Official NuGet package validation tool
- **dotnet list package**: Dependency analysis
- **dotnet test**: Unit test execution
- **Custom validation scripts**: Package content and integration testing

### Manual Inspection Tools

- **NuGet Package Explorer**: GUI tool for examining package contents
- **ILSpy or Reflector**: Decompile and inspect assemblies
- **Git**: Verify source linking works correctly

## 📈 Package Quality Metrics

### Current Status

- ✅ **Test Coverage**: 39/39 unit tests passing (100%)
- ✅ **API Documentation**: XML documentation for all public APIs
- ✅ **Deterministic Build**: Reproducible builds enabled
- ✅ **Source Linking**: GitHub integration configured
- ✅ **Symbol Package**: Debugging support included
- ✅ **Semantic Versioning**: Proper prerelease versioning

### Target Metrics for Release

- **Package Size**: < 1MB (currently ~200KB)
- **Dependencies**: Minimal, secure, and up-to-date
- **API Stability**: No breaking changes between prereleases
- **Documentation**: Complete XML docs + README

## 🚀 Publication Readiness

Once all validation steps pass:

1. **Create NuGet API Key** at [nuget.org](https://www.nuget.org)
1. **Add to GitHub Secrets** as `NUGET_API_KEY`
1. **Create Version Tag**: `git tag v1.0.0-beta.1`
1. **Push Tag**: `git push origin --tags`
1. **Monitor GitHub Actions** for automated publication

## 🆘 Troubleshooting

### Common Issues

#### Package validation fails

- Check all required files are included
- Verify assembly targets correct framework
- Ensure metadata is complete

#### Integration test fails

- Verify package dependencies are correct
- Check for missing runtime dependencies
- Confirm API surface is properly exposed

#### Security scan fails

- Update vulnerable dependencies
- Consider security implications of dependencies
- Document any necessary exceptions

### Getting Help

- Review [NuGet Package Creation Guide](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package)
- Check [Package Validation documentation](https://docs.microsoft.com/en-us/nuget/reference/errors-and-warnings/)
- Examine successful similar packages for best practices
