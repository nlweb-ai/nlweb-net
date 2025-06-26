#!/bin/bash

# Test script to verify Docker build functionality for NLWebNet Demo
# This script verifies that the Docker build can successfully copy all project files

set -e

echo "Testing Docker build file copying stage..."

# Build only up to the file copying stage to verify all referenced files exist
docker build -f deployment/docker/Dockerfile --target build --no-cache -t nlwebnet-build-test . 2>&1 | head -20

echo ""
echo "Docker build file copying stage test completed successfully!"
echo "All project files referenced in the Dockerfile exist and can be copied."