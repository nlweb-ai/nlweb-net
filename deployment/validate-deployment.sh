#!/bin/bash

# NLWebNet Deployment Validation Script
# This script validates the deployment configurations and Docker setup

set -e

echo "🔍 NLWebNet Deployment Validation"
echo "=================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to validate file exists
validate_file() {
    if [ ! -f "$1" ]; then
        echo "❌ Missing file: $1"
        exit 1
    else
        echo "✅ Found: $1"
    fi
}

# Function to validate directory exists
validate_directory() {
    if [ ! -d "$1" ]; then
        echo "❌ Missing directory: $1"
        exit 1
    else
        echo "✅ Found: $1"
    fi
}

# Check prerequisites
echo ""
echo "📋 Checking Prerequisites..."
echo "----------------------------"

if command_exists docker; then
    echo "✅ Docker is installed: $(docker --version)"
else
    echo "❌ Docker is not installed"
    exit 1
fi

if command_exists kubectl; then
    echo "✅ kubectl is installed: $(kubectl version --client --short 2>/dev/null || echo 'kubectl available')"
else
    echo "⚠️  kubectl is not installed (optional for Kubernetes deployment)"
fi

if command_exists helm; then
    echo "✅ Helm is installed: $(helm version --short 2>/dev/null || echo 'Helm available')"
else
    echo "⚠️  Helm is not installed (optional for Helm deployment)"
fi

# Validate core deployment files
echo ""
echo "📁 Validating Core Files..."
echo "---------------------------"

validate_file "Dockerfile"
validate_file ".dockerignore"
validate_file "docker-compose.yml"

# Validate Kubernetes manifests
echo ""
echo "☸️  Validating Kubernetes Manifests..."
echo "-------------------------------------"

validate_directory "deployment/kubernetes"
validate_file "deployment/kubernetes/deployment.yaml"
validate_file "deployment/kubernetes/service.yaml"
validate_file "deployment/kubernetes/ingress.yaml"
validate_file "deployment/kubernetes/configmap.yaml"
validate_file "deployment/kubernetes/hpa.yaml"

# Validate Azure deployment templates
echo ""
echo "☁️  Validating Azure Templates..."
echo "--------------------------------"

validate_directory "deployment/azure"
validate_file "deployment/azure/container-apps.json"
validate_file "deployment/azure/container-apps.bicep"
validate_file "deployment/azure/app-service.json"

# Validate Helm charts
echo ""
echo "⚓ Validating Helm Charts..."
echo "---------------------------"

validate_directory "deployment/helm/nlwebnet"
validate_file "deployment/helm/nlwebnet/Chart.yaml"
validate_file "deployment/helm/nlwebnet/values.yaml"
validate_directory "deployment/helm/nlwebnet/templates"

# Validate supporting files
echo ""
echo "📚 Validating Documentation..."
echo "-----------------------------"

validate_file "deployment/README.md"
validate_file "deployment/docker-compose-examples.md"

# Validate Docker build (basic syntax check)
echo ""
echo "🐳 Validating Docker Configuration..."
echo "-----------------------------------"

# Check if Dockerfile exists and has basic structure
if grep -q "FROM" Dockerfile && grep -q "COPY" Dockerfile && grep -q "RUN" Dockerfile; then
    echo "✅ Dockerfile has basic structure"
else
    echo "❌ Dockerfile missing basic elements"
    exit 1
fi

# Validate docker-compose file
if command_exists docker-compose || docker compose version >/dev/null 2>&1; then
    if docker-compose config >/dev/null 2>&1 || docker compose config >/dev/null 2>&1; then
        echo "✅ docker-compose.yml is valid"
    else
        echo "❌ docker-compose.yml has issues"
        exit 1
    fi
else
    echo "⚠️  docker-compose not available for validation"
fi

# Validate Kubernetes manifests syntax (basic YAML validation)
if command_exists kubectl; then
    echo ""
    echo "☸️  Validating Kubernetes Syntax..."
    echo "---------------------------------"
    
    echo "⚠️  Using basic YAML validation (no cluster connection)"
    
    # Basic YAML validation for K8s manifests
    for file in deployment/kubernetes/*.yaml; do
        if python3 -c "
import yaml
try:
    with open('$file', 'r') as f:
        list(yaml.safe_load_all(f))
    print('Valid')
except Exception as e:
    print(f'Invalid: {e}')
    exit(1)
" >/dev/null 2>&1; then
            echo "✅ Valid YAML: $(basename "$file")"
        else
            echo "❌ Invalid YAML: $(basename "$file")"
            exit 1
        fi
    done
fi

# Validate Helm chart (if helm available)
if command_exists helm; then
    echo ""
    echo "⚓ Validating Helm Chart..."
    echo "-------------------------"
    
    if helm lint deployment/helm/nlwebnet/ >/dev/null 2>&1; then
        echo "✅ Helm chart is valid"
    else
        echo "❌ Helm chart has issues"
        helm lint deployment/helm/nlwebnet/
        exit 1
    fi
fi

# Final summary
echo ""
echo "🎉 Validation Complete!"
echo "======================"
echo "✅ All deployment configurations are valid"
echo ""
echo "📋 Available Deployment Options:"
echo "  🐳 Docker: docker-compose up -d"
echo "  ☸️  Kubernetes: kubectl apply -f deployment/kubernetes/"
echo "  ⚓ Helm: helm install nlwebnet deployment/helm/nlwebnet/"
echo "  ☁️  Azure: Use templates in deployment/azure/"
echo ""
echo "📖 For detailed instructions, see: deployment/README.md"