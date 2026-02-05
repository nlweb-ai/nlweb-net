#!/bin/bash
# Smoke test script for NLWebNet Docker container
# Tests basic functionality and health endpoints

set -euo pipefail

CONTAINER_NAME="nlwebnet-test-$(date +%s)"
CONTAINER_PORT="8080"
HOST_PORT="8081"
IMAGE_NAME="${1:-nlwebnet-demo:latest}"
MAX_WAIT_TIME=30

echo "🧪 Starting smoke test for NLWebNet Docker container"
echo "📦 Testing image: $IMAGE_NAME"

# Function to cleanup container on exit
cleanup() {
    echo "🧹 Cleaning up test container..."
    docker stop "$CONTAINER_NAME" >/dev/null 2>&1 || true
    docker rm "$CONTAINER_NAME" >/dev/null 2>&1 || true
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Start the container
echo "🚀 Starting container..."
if ! docker run -d --name "$CONTAINER_NAME" -p "$HOST_PORT:$CONTAINER_PORT" --memory=512m --cpus=1 "$IMAGE_NAME"; then
    echo "❌ Failed to start container"
    exit 1
fi

echo "⏳ Waiting for container to be ready..."

# Wait for container to be ready
wait_time=0
while [ $wait_time -lt $MAX_WAIT_TIME ]; do
    if docker ps | grep "$CONTAINER_NAME" >/dev/null; then
        echo "✅ Container is running"
        break
    fi
    
    sleep 1
    wait_time=$((wait_time + 1))
    
    if [ $wait_time -eq $MAX_WAIT_TIME ]; then
        echo "❌ Container failed to start within $MAX_WAIT_TIME seconds"
        echo "📋 Container logs:"
        docker logs "$CONTAINER_NAME"
        exit 1
    fi
done

# Wait a bit more for the application to start
echo "⏳ Waiting for application to initialize..."
sleep 5

# Test basic health endpoint
echo "🔍 Testing health endpoint..."
if curl --max-time 10 -f -s "http://localhost:$HOST_PORT/health" >/dev/null; then
    echo "✅ Health endpoint responds successfully"
    
    # Get and display health response
    health_response=$(curl --max-time 10 -s "http://localhost:$HOST_PORT/health")
    echo "📊 Health response: $health_response"
else
    echo "❌ Health endpoint failed"
    echo "📋 Container logs:"
    docker logs "$CONTAINER_NAME"
    exit 1
fi

# Test if detailed health endpoint exists
echo "🔍 Testing detailed health endpoint..."
if curl --max-time 10 -f -s "http://localhost:$HOST_PORT/health/detailed" >/dev/null; then
    echo "✅ Detailed health endpoint responds successfully"
    detailed_health=$(curl --max-time 10 -s "http://localhost:$HOST_PORT/health/detailed")
    echo "📊 Detailed health response: $detailed_health"
else
    echo "⚠️ Detailed health endpoint not available or failed (this may be expected)"
fi

# Test basic root endpoint
echo "🔍 Testing root endpoint..."
if curl --max-time 10 -f -s "http://localhost:$HOST_PORT/" >/dev/null; then
    echo "✅ Root endpoint responds successfully"
else
    echo "⚠️ Root endpoint failed (this may be expected for API-only services)"
fi

# Test if NLWebNet API endpoints are available
echo "🔍 Testing NLWebNet API endpoints..."
if curl --max-time 10 -f -s "http://localhost:$HOST_PORT/api/nlweb" >/dev/null 2>&1; then
    echo "✅ NLWebNet API endpoint responds"
elif curl --max-time 10 -f -s "http://localhost:$HOST_PORT/nlweb" >/dev/null 2>&1; then
    echo "✅ NLWebNet endpoint responds"
else
    echo "⚠️ NLWebNet API endpoints may not be available or configured differently"
fi

# Display final container status
echo "📊 Final container status:"
docker ps | grep "$CONTAINER_NAME" || echo "Container not found in ps output"

echo "📋 Recent container logs:"
docker logs --tail 10 "$CONTAINER_NAME"

echo "🎉 Smoke test completed successfully!"
echo "💡 Container is working correctly and responding to requests"
echo "🔗 You can access the application at: http://localhost:$HOST_PORT"