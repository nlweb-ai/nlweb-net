#!/bin/bash
# Build script for Docker container in environments with SSL issues
# This script prepares a build-ready environment and builds the Docker image

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

# Build the Docker image with appropriate strategy based on environment
echo "🐳 Building Docker image..."

# Build the Docker image
if docker build -f deployment/docker/Dockerfile . -t nlwebnet-demo:latest; then
    echo "✅ Docker build successful"
else
    echo "❌ Docker build failed"
    exit 1
fi

echo "🎉 Docker image build completed successfully!"
echo "📝 To run the container: docker run -p 8080:8080 nlwebnet-demo:latest"
echo "🔍 To test health endpoint: curl http://localhost:8080/health"