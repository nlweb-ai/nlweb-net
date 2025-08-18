#!/bin/bash
# Build script for Docker container in environments with SSL issues
# This script prepares a build-ready environment and builds the Docker image

set -e

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

# Try the standard approach first
if docker build -f deployment/docker/Dockerfile . -t nlwebnet-demo:latest; then
    echo "✅ Docker build successful with standard approach"
elif [ -f "deployment/docker/Dockerfile.pre-built" ]; then
    echo "⚠️ Standard build failed, trying pre-built approach..."
    # Use the pre-built approach if available
    cp deployment/docker/.dockerignore-prebuilt .dockerignore.backup
    mv .dockerignore .dockerignore.original
    cp deployment/docker/.dockerignore-prebuilt .dockerignore
    
    if docker build -f deployment/docker/Dockerfile.pre-built . -t nlwebnet-demo:latest; then
        echo "✅ Docker build successful with pre-built approach"
    else
        echo "❌ Both build approaches failed"
        # Restore original .dockerignore
        mv .dockerignore.original .dockerignore
        rm -f .dockerignore.backup
        exit 1
    fi
    
    # Restore original .dockerignore
    mv .dockerignore.original .dockerignore
    rm -f .dockerignore.backup
else
    echo "❌ Docker build failed and no fallback available"
    exit 1
fi

echo "🎉 Docker image build completed successfully!"
echo "📝 To run the container: docker run -p 8080:8080 nlwebnet-demo:latest"
echo "🔍 To test health endpoint: curl http://localhost:8080/health"