#!/bin/bash
# Build script for Docker container in environments with SSL issues
# This script provides multiple approaches for building Docker images

set -euo pipefail

echo "🔧 Building NLWebNet Docker image with SSL workarounds..."

# Check if we're in the right directory
if [ ! -f "NLWebNet.sln" ]; then
    echo "❌ Error: Must be run from repository root directory"
    exit 1
fi

# Pre-restore packages locally to avoid SSL issues in Docker
echo "📦 Pre-restoring packages locally..."
dotnet restore

echo "🐳 Building Docker image..."

# Try standard Dockerfile first (for CI environments)
if docker build -f deployment/docker/Dockerfile . -t nlwebnet-demo:latest 2>/dev/null; then
    echo "✅ Docker build successful with standard Dockerfile"
# Fall back to local Dockerfile (for SSL-restricted environments)  
elif docker build -f deployment/docker/Dockerfile.local . -t nlwebnet-demo:latest; then
    echo "✅ Docker build successful with local Dockerfile (SSL workaround)"
else
    echo "❌ Both Docker build approaches failed"
    echo ""
    echo "🔍 Troubleshooting suggestions:"
    echo "  1. Use pre-built images from GHCR:"
    echo "     docker pull ghcr.io/nlweb-ai/nlweb-net/demo:latest"
    echo ""
    echo "  2. Check SSL certificate issues:"
    echo "     - Update system certificates: sudo update-ca-certificates"  
    echo "     - Clear NuGet cache: dotnet nuget locals all --clear"
    echo ""
    echo "  3. Verify network connectivity:"
    echo "     - Test: ping api.nuget.org"
    echo "     - Check corporate proxy/firewall settings"
    echo ""
    echo "  4. Check Docker environment:"
    echo "     - Ensure Docker has sufficient disk space"
    echo "     - Verify Docker version: docker --version"
    exit 1
fi

echo ""
echo "🎉 Docker image build completed successfully!"
echo "📝 To run the container:"
echo "   docker run -p 8080:8080 nlwebnet-demo:latest"
echo ""
echo "🔍 To test health endpoint:"
echo "   curl http://localhost:8080/health"
echo ""
echo "🧪 To run smoke tests:"
echo "   ./deployment/docker/smoke-test.sh nlwebnet-demo:latest"