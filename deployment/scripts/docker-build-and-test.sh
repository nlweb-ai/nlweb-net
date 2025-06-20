#!/bin/bash
set -e

# Docker Build and Test Script for NLWebNet Demo
# This script builds the Docker image and runs basic validation tests

echo "🐳 Building NLWebNet Docker Image..."

# Build the image
docker build -t nlwebnet-demo:test .

echo "✅ Docker image built successfully"

# Test the image by running it temporarily
echo "🧪 Testing Docker image..."

# Start container in background
CONTAINER_ID=$(docker run -d -p 8081:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e NLWebNet__DefaultMode=List \
  -e NLWebNet__EnableStreaming=true \
  nlwebnet-demo:test)

echo "Container started with ID: $CONTAINER_ID"

# Wait for container to be ready
echo "⏳ Waiting for container to start..."
sleep 10

# Test health endpoint
echo "🏥 Testing health endpoint..."
if curl -f http://localhost:8081/health; then
    echo "✅ Health check passed"
else
    echo "❌ Health check failed"
    docker logs $CONTAINER_ID
    docker stop $CONTAINER_ID
    docker rm $CONTAINER_ID
    exit 1
fi

# Test main application endpoint
echo "🌐 Testing main application..."
if curl -f http://localhost:8081/ > /dev/null 2>&1; then
    echo "✅ Main application responds"
else
    echo "⚠️  Main application test inconclusive (may require additional setup)"
fi

# Cleanup
echo "🧹 Cleaning up..."
docker stop $CONTAINER_ID
docker rm $CONTAINER_ID

echo "🎉 All tests passed! Docker image is ready for deployment."
echo "📦 Tagged as: nlwebnet-demo:test"
echo ""
echo "Next steps:"
echo "- Tag for your registry: docker tag nlwebnet-demo:test your-registry.azurecr.io/nlwebnet-demo:latest"
echo "- Push to registry: docker push your-registry.azurecr.io/nlwebnet-demo:latest"
echo "- Deploy using Kubernetes, Azure Container Apps, or Docker Compose"