#!/bin/bash

# NLWebNet Azure Deployment Script
# This script deploys NLWebNet to Azure using Bicep templates

set -e  # Exit on any error

# Configuration
RESOURCE_GROUP=""
LOCATION="eastus"
APP_NAME="nlwebnet"
ENVIRONMENT="dev"
TEMPLATE_FILE=""
PARAMETERS_FILE=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

print_header() {
    echo -e "${BLUE}[DEPLOY]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Required Options:"
    echo "  -g, --resource-group NAME    Azure resource group name"
    echo "  -t, --template TEMPLATE      Deployment template (container-apps|app-service)"
    echo ""
    echo "Optional Options:"
    echo "  -l, --location LOCATION      Azure location (default: $LOCATION)"
    echo "  -a, --app-name NAME          Application name (default: $APP_NAME)"
    echo "  -e, --environment ENV        Environment (dev|staging|prod) (default: $ENVIRONMENT)"
    echo "  -p, --parameters FILE        Parameters file path"
    echo "  -h, --help                   Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 -g myResourceGroup -t container-apps"
    echo "  $0 -g myResourceGroup -t app-service -e prod"
    echo "  $0 -g myResourceGroup -t container-apps -p my-params.json"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -g|--resource-group)
            RESOURCE_GROUP="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -a|--app-name)
            APP_NAME="$2"
            shift 2
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -t|--template)
            case "$2" in
                container-apps)
                    TEMPLATE_FILE="deploy/azure/container-apps.bicep"
                    PARAMETERS_FILE="deploy/azure/container-apps.parameters.json"
                    ;;
                app-service)
                    TEMPLATE_FILE="deploy/azure/app-service.bicep"
                    ;;
                *)
                    print_error "Invalid template: $2. Use 'container-apps' or 'app-service'"
                    exit 1
                    ;;
            esac
            shift 2
            ;;
        -p|--parameters)
            PARAMETERS_FILE="$2"
            shift 2
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

# Validate required parameters
if [[ -z "$RESOURCE_GROUP" ]]; then
    print_error "Resource group is required. Use -g/--resource-group option."
    show_usage
    exit 1
fi

if [[ -z "$TEMPLATE_FILE" ]]; then
    print_error "Template is required. Use -t/--template option."
    show_usage
    exit 1
fi

# Check if template file exists
if [[ ! -f "$TEMPLATE_FILE" ]]; then
    print_error "Template file not found: $TEMPLATE_FILE"
    exit 1
fi

# Check if parameters file exists (if specified)
if [[ -n "$PARAMETERS_FILE" && ! -f "$PARAMETERS_FILE" ]]; then
    print_error "Parameters file not found: $PARAMETERS_FILE"
    exit 1
fi

print_header "🚀 Starting Azure deployment for NLWebNet"
print_status "Resource Group: $RESOURCE_GROUP"
print_status "Location: $LOCATION"
print_status "App Name: $APP_NAME"
print_status "Environment: $ENVIRONMENT"
print_status "Template: $TEMPLATE_FILE"

if [[ -n "$PARAMETERS_FILE" ]]; then
    print_status "Parameters: $PARAMETERS_FILE"
fi

# Check if Azure CLI is installed and user is logged in
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    print_status "Installation: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

print_status "✅ Azure CLI is available and user is authenticated"

# Check if resource group exists, create if it doesn't
print_status "Checking resource group: $RESOURCE_GROUP"
if ! az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    print_warning "Resource group '$RESOURCE_GROUP' does not exist. Creating it..."
    if az group create --name "$RESOURCE_GROUP" --location "$LOCATION"; then
        print_status "✅ Resource group created successfully"
    else
        print_error "❌ Failed to create resource group"
        exit 1
    fi
else
    print_status "✅ Resource group exists"
fi

# Build deployment command
DEPLOYMENT_NAME="${APP_NAME}-deployment-$(date +%Y%m%d-%H%M%S)"
DEPLOY_CMD="az deployment group create"
DEPLOY_CMD="$DEPLOY_CMD --resource-group $RESOURCE_GROUP"
DEPLOY_CMD="$DEPLOY_CMD --name $DEPLOYMENT_NAME"
DEPLOY_CMD="$DEPLOY_CMD --template-file $TEMPLATE_FILE"

# Add inline parameters
DEPLOY_CMD="$DEPLOY_CMD --parameters appName=$APP_NAME"
DEPLOY_CMD="$DEPLOY_CMD --parameters location=$LOCATION"
DEPLOY_CMD="$DEPLOY_CMD --parameters environment=$ENVIRONMENT"

# Add parameters file if specified
if [[ -n "$PARAMETERS_FILE" ]]; then
    DEPLOY_CMD="$DEPLOY_CMD --parameters @$PARAMETERS_FILE"
fi

print_status "Executing deployment..."
print_status "Deployment name: $DEPLOYMENT_NAME"

# Execute the deployment
if eval "$DEPLOY_CMD"; then
    print_status "✅ Deployment completed successfully!"
    
    # Get deployment outputs
    print_status "Retrieving deployment outputs..."
    if az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query properties.outputs &> /dev/null; then
        OUTPUTS=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query properties.outputs -o json)
        echo "$OUTPUTS" | jq -r 'to_entries[] | "  \(.key): \(.value.value)"' 2>/dev/null || echo "$OUTPUTS"
    fi
else
    print_error "❌ Deployment failed"
    exit 1
fi

print_header "🎉 Deployment completed successfully!"
print_status "You can monitor your deployment in the Azure Portal:"
print_status "https://portal.azure.com/#@/resource/subscriptions/*/resourceGroups/$RESOURCE_GROUP"

echo ""
print_status "Next steps:"
echo "  • Check application health endpoint"
echo "  • Configure custom domain (if needed)"
echo "  • Set up monitoring and alerts"
echo "  • Configure CI/CD pipeline"