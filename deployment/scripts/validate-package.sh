#!/bin/bash
# NLWebNet Package Validation Script (Linux/macOS)
# Run this script before publishing to NuGet.org

set -e

echo "🔍 NLWebNet Package Validation Script"
echo "====================================="

# 1. Build and Test
echo -e "\n📦 Step 1: Building and Testing..."
dotnet build ../../src/NLWebNet --configuration Release
dotnet test --configuration Release --no-build

# 2. Create Package  
echo -e "\n📦 Step 2: Creating Package..."
OUTPUT_DIR="../../packages-validation"
rm -rf "$OUTPUT_DIR"
dotnet pack ../../src/NLWebNet --configuration Release --output "$OUTPUT_DIR"

# Find the created package
NUPKG_FILE=$(find "$OUTPUT_DIR" -name "*.nupkg" ! -name "*.symbols.nupkg" | head -n 1)
SNUPKG_FILE=$(find "$OUTPUT_DIR" -name "*.symbols.nupkg" | head -n 1)

if [ -z "$NUPKG_FILE" ]; then
    echo "❌ No .nupkg file found"
    exit 1
fi

echo "✅ Package created: $(basename "$NUPKG_FILE")"
if [ -n "$SNUPKG_FILE" ]; then
    echo "✅ Symbols package created: $(basename "$SNUPKG_FILE")"
else
    echo "⚠️ No symbols package found"
fi

# 3. Package Content Validation
echo -e "\n📦 Step 3: Validating Package Content..."
TEMP_DIR="./temp-package-extract"
rm -rf "$TEMP_DIR"
unzip -q "$NUPKG_FILE" -d "$TEMP_DIR"

# Check for required files
REQUIRED_FILES=(
    "lib/net9.0/NLWebNet.dll"
    "README.md"
    "_rels/.rels"
    "NLWebNet.nuspec"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$TEMP_DIR/$file" ]; then
        echo "✅ Found: $file"
    else
        echo "❌ Missing: $file"
        exit 1
    fi
done

# 4. NuGet Package Validation Tool
echo -e "\n📦 Step 4: Running NuGet Package Validation..."
if ! command -v dotnet-validate &> /dev/null; then
    echo "Installing dotnet-validate..."
    dotnet tool install --global dotnet-validate --version 0.0.1-preview.537
fi

if dotnet validate package local "$NUPKG_FILE"; then
    echo "✅ Package validation passed"
else
    echo "⚠️ Package validation warnings detected"
fi

# 5. Dependency Analysis
echo -e "\n📦 Step 5: Analyzing Dependencies..."
echo "Checking for vulnerable dependencies..."
if dotnet list ../../src/NLWebNet package --vulnerable --include-transitive 2>&1 | grep -q "has the following vulnerable packages"; then
    echo "❌ Vulnerable dependencies found"
    dotnet list ../../src/NLWebNet package --vulnerable --include-transitive
    exit 1
else
    echo "✅ No vulnerable dependencies found"
fi

echo "Checking for deprecated dependencies..."
if dotnet list ../../src/NLWebNet package --deprecated 2>&1 | grep -q "has the following deprecated packages"; then
    echo "⚠️ Deprecated dependencies found"
    dotnet list ../../src/NLWebNet package --deprecated
else
    echo "✅ No deprecated dependencies found"
fi

# 6. Integration Test
echo -e "\n📦 Step 6: Integration Test..."
TEST_CONSUMER_DIR="./temp-test-consumer"
rm -rf "$TEST_CONSUMER_DIR"

# Create test consumer project
dotnet new web -n TestConsumer -o "$TEST_CONSUMER_DIR" --force
cd "$TEST_CONSUMER_DIR"

# Add the local package
dotnet add package NLWebNet --source ../../packages-validation --prerelease --prerelease

# Create test Program.cs
cat > Program.cs << 'EOF'
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
EOF

# Test compilation
echo "Testing compilation of consumer project..."
if dotnet build; then
    echo "✅ Integration test passed - consumer project compiles successfully"
else
    echo "❌ Test consumer compilation failed"
    cd ..
    exit 1
fi

cd ..

# 7. Package Size Check
echo -e "\n📦 Step 7: Package Size Analysis..."
PACKAGE_SIZE=$(stat -c%s "$NUPKG_FILE")
PACKAGE_SIZE_KB=$((PACKAGE_SIZE / 1024))
echo "📦 Package size: ${PACKAGE_SIZE_KB} KB"

if [ $PACKAGE_SIZE_KB -gt 1000 ]; then
    echo "⚠️ Package is quite large (>1MB). Consider optimization."
elif [ $PACKAGE_SIZE_KB -gt 500 ]; then
    echo "⚠️ Package is moderately large (>500KB). Review content."
else
    echo "✅ Package size is reasonable"
fi

# Cleanup
echo -e "\n🧹 Cleaning up..."
rm -rf "$TEMP_DIR" "$TEST_CONSUMER_DIR" "$OUTPUT_DIR"

echo -e "\n🎉 Package validation completed successfully!"
echo "📦 Package is ready for publication to NuGet.org"
echo -e "\n💡 Next steps:"
echo "   1. Add NUGET_API_KEY to GitHub secrets"
echo "   2. Create and push version tag: git tag v1.0.0-beta.1 && git push origin --tags"
echo "   3. Monitor GitHub Actions for automated publication"
