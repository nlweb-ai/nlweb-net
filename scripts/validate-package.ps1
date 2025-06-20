# NLWebNet Package Validation Script
# Run this script before publishing to NuGet.org

param(
    [string]$PackagePath = "",
    [switch]$SkipDependencyCheck = $false
)

$ErrorActionPreference = "Stop"
Write-Host "🔍 NLWebNet Package Validation Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Function to check if a command exists
function Test-Command($command) {
    $null = Get-Command $command -ErrorAction SilentlyContinue
    return $?
}

# 1. Build and Test
Write-Host "`n📦 Step 1: Building and Testing..." -ForegroundColor Yellow
dotnet build src/NLWebNet --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

dotnet test --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

# 2. Create Package
Write-Host "`n📦 Step 2: Creating Package..." -ForegroundColor Yellow
$outputDir = ".\packages-validation"
Remove-Item $outputDir -Recurse -Force -ErrorAction SilentlyContinue
dotnet pack src/NLWebNet --configuration Release --output $outputDir
if ($LASTEXITCODE -ne 0) { throw "Pack failed" }

# Find the created package
$nupkgFile = Get-ChildItem "$outputDir\*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.nupkg" } | Select-Object -First 1
if (-not $nupkgFile) { throw "No .nupkg file found" }

$snupkgFile = Get-ChildItem "$outputDir\*.symbols.nupkg" | Select-Object -First 1

Write-Host "✅ Package created: $($nupkgFile.Name)" -ForegroundColor Green
if ($snupkgFile) {
    Write-Host "✅ Symbols package created: $($snupkgFile.Name)" -ForegroundColor Green
} else {
    Write-Host "⚠️ No symbols package found" -ForegroundColor Red
}

# 3. Package Content Validation
Write-Host "`n📦 Step 3: Validating Package Content..." -ForegroundColor Yellow

# Extract and examine package contents
$tempDir = ".\temp-package-extract"
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive $nupkgFile.FullName -DestinationPath $tempDir

# Check for required files
$requiredFiles = @(
    "lib\net9.0\NLWebNet.dll",
    "README.md",
    "_rels\.rels",
    "NLWebNet.nuspec"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $tempDir $file
    if (Test-Path $filePath) {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
        throw "Required file missing: $file"
    }
}

# Check assembly metadata
$assemblyPath = Join-Path $tempDir "lib\net9.0\NLWebNet.dll"
if (Test-Path $assemblyPath) {
    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $version = $assembly.GetName().Version
    Write-Host "✅ Assembly version: $version" -ForegroundColor Green
    
    # Check for key types
    $keyTypes = @(
        "NLWebNet.Models.NLWebRequest",
        "NLWebNet.Models.NLWebResponse", 
        "NLWebNet.Services.INLWebService",        "NLWebNet.ServiceCollectionExtensions",
        "NLWebNet.ApplicationBuilderExtensions"
    )
    
    foreach ($typeName in $keyTypes) {
        $type = $assembly.GetType($typeName)
        if ($type) {
            Write-Host "✅ Found type: $typeName" -ForegroundColor Green
        } else {
            Write-Host "❌ Missing type: $typeName" -ForegroundColor Red
            throw "Key type missing: $typeName"
        }
    }
}

# 4. NuGet Package Validation Tool
Write-Host "`n📦 Step 4: Running NuGet Package Validation..." -ForegroundColor Yellow
if (Test-Command "dotnet-validate") {
    dotnet validate package local $nupkgFile.FullName
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "⚠️ Package validation warnings detected" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Package validation passed" -ForegroundColor Green
    }
} else {
    Write-Host "Installing dotnet-validate..."
    dotnet tool install --global dotnet-validate --version 0.0.1-preview.537
    dotnet validate package local $nupkgFile.FullName
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "⚠️ Package validation warnings detected" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Package validation passed" -ForegroundColor Green
    }
}

# 5. Dependency Analysis  
Write-Host "`n📦 Step 5: Analyzing Dependencies..." -ForegroundColor Yellow
if (-not $SkipDependencyCheck) {
    # Check for vulnerable dependencies
    Write-Host "Checking for vulnerable dependencies..."
    $vulnerableOutput = dotnet list src/NLWebNet package --vulnerable --include-transitive 2>&1
    if ($vulnerableOutput -match "has the following vulnerable packages") {
        Write-Host "❌ Vulnerable dependencies found:" -ForegroundColor Red
        Write-Host $vulnerableOutput -ForegroundColor Red
        throw "Vulnerable dependencies detected"
    } else {
        Write-Host "✅ No vulnerable dependencies found" -ForegroundColor Green
    }
    
    # Check for deprecated dependencies
    Write-Host "Checking for deprecated dependencies..."
    $deprecatedOutput = dotnet list src/NLWebNet package --deprecated 2>&1
    if ($deprecatedOutput -match "has the following deprecated packages") {
        Write-Host "⚠️ Deprecated dependencies found:" -ForegroundColor Yellow
        Write-Host $deprecatedOutput -ForegroundColor Yellow
    } else {
        Write-Host "✅ No deprecated dependencies found" -ForegroundColor Green
    }
}

# 6. Integration Test (Create Test Consumer)
Write-Host "`n📦 Step 6: Integration Test..." -ForegroundColor Yellow
$testConsumerDir = ".\temp-test-consumer"
Remove-Item $testConsumerDir -Recurse -Force -ErrorAction SilentlyContinue

# Create a minimal test consumer project
dotnet new web -n TestConsumer -o $testConsumerDir --force
Set-Location $testConsumerDir

# Add the local package
dotnet add package NLWebNet --source ..\packages-validation --prerelease

# Create a test Program.cs
$testProgram = @'
using NLWebNet;

var builder = WebApplication.CreateBuilder(args);

// Test NLWebNet integration
builder.Services.AddNLWebNet(options =>
{
    options.DefaultMode = NLWebNet.Models.QueryMode.List;
    options.EnableStreaming = true;
});

var app = builder.Build();

// Test NLWebNet endpoints  
app.MapNLWebNet();

app.MapGet("/", () => "Test consumer with NLWebNet integration works!");

app.Run();
'@

$testProgram | Out-File -FilePath "Program.cs" -Encoding UTF8

# Test compilation
Write-Host "Testing compilation of consumer project..."
dotnet build
if ($LASTEXITCODE -ne 0) { 
    Set-Location ..
    throw "Test consumer compilation failed" 
}

Write-Host "✅ Integration test passed - consumer project compiles successfully" -ForegroundColor Green
Set-Location ..

# 7. Package Size Check
Write-Host "`n📦 Step 7: Package Size Analysis..." -ForegroundColor Yellow
$packageSize = ($nupkgFile.Length / 1KB)
Write-Host "📦 Package size: $([math]::Round($packageSize, 2)) KB" -ForegroundColor Cyan

if ($packageSize -gt 1000) {
    Write-Host "⚠️ Package is quite large (>1MB). Consider optimization." -ForegroundColor Yellow
} elseif ($packageSize -gt 500) {
    Write-Host "⚠️ Package is moderately large (>500KB). Review content." -ForegroundColor Yellow
} else {
    Write-Host "✅ Package size is reasonable" -ForegroundColor Green
}

# 8. Metadata Validation
Write-Host "`n📦 Step 8: Metadata Validation..." -ForegroundColor Yellow
$nuspecPath = Join-Path $tempDir "NLWebNet.nuspec"
if (Test-Path $nuspecPath) {
    $nuspec = [xml](Get-Content $nuspecPath)
    $metadata = $nuspec.package.metadata
    
    $checks = @{
        "ID" = $metadata.id
        "Version" = $metadata.version  
        "Authors" = $metadata.authors
        "Description" = $metadata.description
        "ProjectURL" = $metadata.projectUrl
        "RepositoryURL" = $metadata.repository.url
        "License" = $metadata.license.type
        "Tags" = $metadata.tags
    }
    
    foreach ($check in $checks.GetEnumerator()) {
        if ($check.Value) {
            Write-Host "✅ $($check.Key): $($check.Value)" -ForegroundColor Green
        } else {
            Write-Host "❌ Missing: $($check.Key)" -ForegroundColor Red
        }
    }
}

# Cleanup
Write-Host "`n🧹 Cleaning up..." -ForegroundColor Yellow
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $testConsumerDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $outputDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n🎉 Package validation completed successfully!" -ForegroundColor Green
Write-Host "📦 Package is ready for publication to NuGet.org" -ForegroundColor Cyan
Write-Host "`n💡 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Add NUGET_API_KEY to GitHub secrets" -ForegroundColor White
Write-Host "   2. Create and push version tag: git tag v1.0.0-beta.1 && git push origin --tags" -ForegroundColor White
Write-Host "   3. Monitor GitHub Actions for automated publication" -ForegroundColor White
