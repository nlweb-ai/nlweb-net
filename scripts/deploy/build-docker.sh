#!/bin/bash

# NLWebNet Docker Build and Deploy Script
# This script builds the Docker image and optionally pushes it to a registry

set -e  # Exit on any error

# Configuration
IMAGE_NAME="nlwebnet-demo"
DEFAULT_TAG="latest"
REGISTRY=""
DOCKERFILE="Dockerfile"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -t, --tag TAG         Docker image tag (default: $DEFAULT_TAG)"
    echo "  -r, --registry REG    Container registry URL (e.g., myregistry.azurecr.io)"
    echo "  -p, --push           Push image to registry after building"
    echo "  -n, --no-cache       Build without using cache"
    echo "  -h, --help           Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Build with default tag"
    echo "  $0 -t v1.0.0                        # Build with specific tag"
    echo "  $0 -r myregistry.azurecr.io -p      # Build and push to registry"
    echo "  $0 -t v1.0.0 -r myregistry.azurecr.io -p  # Build specific version and push"
}

# Parse command line arguments
TAG="$DEFAULT_TAG"
PUSH=false
NO_CACHE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--tag)
            TAG="$2"
            shift 2
            ;;
        -r|--registry)
            REGISTRY="$2"
            shift 2
            ;;
        -p|--push)
            PUSH=true
            shift
            ;;
        -n|--no-cache)
            NO_CACHE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Build the full image name
if [[ -n "$REGISTRY" ]]; then
    FULL_IMAGE_NAME="$REGISTRY/$IMAGE_NAME:$TAG"
else
    FULL_IMAGE_NAME="$IMAGE_NAME:$TAG"
fi

print_status "Starting Docker build process..."
print_status "Image name: $FULL_IMAGE_NAME"
print_status "Dockerfile: $DOCKERFILE"

# Check if Dockerfile exists
if [[ ! -f "$DOCKERFILE" ]]; then
    print_error "Dockerfile not found: $DOCKERFILE"
    exit 1
fi

# Build Docker build command
BUILD_CMD="docker build"

if [[ "$NO_CACHE" == true ]]; then
    BUILD_CMD="$BUILD_CMD --no-cache"
fi

BUILD_CMD="$BUILD_CMD -t $FULL_IMAGE_NAME -f $DOCKERFILE ."

print_status "Running: $BUILD_CMD"

# Execute the build
if eval "$BUILD_CMD"; then
    print_status "✅ Docker image built successfully: $FULL_IMAGE_NAME"
else
    print_error "❌ Docker build failed"
    exit 1
fi

# Push to registry if requested
if [[ "$PUSH" == true ]]; then
    if [[ -z "$REGISTRY" ]]; then
        print_error "Cannot push: no registry specified. Use -r/--registry option."
        exit 1
    fi
    
    print_status "Pushing image to registry..."
    
    if docker push "$FULL_IMAGE_NAME"; then
        print_status "✅ Image pushed successfully to $REGISTRY"
    else
        print_error "❌ Failed to push image to registry"
        exit 1
    fi
fi

# Show final summary
echo ""
print_status "🎉 Build process completed successfully!"
print_status "Built image: $FULL_IMAGE_NAME"

if [[ "$PUSH" == true ]]; then
    print_status "Image available at: $REGISTRY/$IMAGE_NAME:$TAG"
fi

echo ""
print_status "Next steps:"
echo "  • Run locally: docker run -p 8080:8080 $FULL_IMAGE_NAME"
echo "  • Check health: curl http://localhost:8080/health"
echo "  • View app: http://localhost:8080"

if [[ "$PUSH" == false && -n "$REGISTRY" ]]; then
    echo "  • Push to registry: $0 -t $TAG -r $REGISTRY -p"
fi