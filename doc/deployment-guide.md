# NLWebNet Deployment Guide

This guide provides comprehensive instructions for deploying the NLWebNet demo application across various platforms and environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [.NET Aspire Deployment (Recommended)](#net-aspire-deployment-recommended)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Azure Deployment](#azure-deployment)
- [Helm Deployment](#helm-deployment)
- [Environment Configuration](#environment-configuration)
- [Monitoring and Health Checks](#monitoring-and-health-checks)

## Prerequisites

- .NET 8.0 SDK or later
- .NET Aspire workload: `dotnet workload install aspire`
- Docker and Docker Compose (for containerization)
- Kubernetes cluster (for K8s deployment)
- Azure CLI (for Azure deployment)
- Helm 3.x (for Helm deployment)

## .NET Aspire Deployment (Recommended)

**.NET Aspire is the recommended approach for .NET developers** building cloud-native applications. It provides built-in observability, service discovery, and production-ready patterns.

### Quick Start

```bash
# Install Aspire workload (one-time setup)
dotnet workload install aspire

# Run the application with Aspire orchestration
cd demo-apphost
dotnet run
```

### Features

- **Aspire Dashboard**: Visual monitoring at `https://localhost:15888`
- **Built-in Observability**: Distributed tracing, metrics, and health checks
- **Service Discovery**: Automatic service location and communication
- **Development Experience**: Hot reload, centralized logging, and debugging

### Benefits over Traditional Containerization

1. **Integrated Development**: Built-in dashboard and debugging tools
2. **Production Patterns**: Standardized health checks, resilience, and telemetry
3. **Cloud-Native Ready**: Designed for modern distributed applications
4. **Microsoft Ecosystem**: First-class support in Azure and Visual Studio

📖 **[Complete Aspire Integration Guide](aspire-integration.md)**

## Docker Deployment

### Building the Container Image

```bash
# Build the Docker image
docker build -t nlwebnet-demo:latest .

# Run locally for testing
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e NLWebNet__DefaultMode=List \
  -e NLWebNet__EnableStreaming=true \
  nlwebnet-demo:latest
```

### Using Docker Compose

#### Development Environment
```bash
# Start development environment with hot reload
docker-compose up --build
```

#### Production Environment
```bash
# Start production environment
docker-compose --profile production up -d
```

### Environment Variables

Set these environment variables for production deployment:

```bash
# Application settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
NLWebNet__DefaultMode=List
NLWebNet__EnableStreaming=true
NLWebNet__DefaultTimeoutSeconds=30
NLWebNet__MaxResultsPerQuery=50

# Azure OpenAI (if using)
AzureOpenAI__ApiKey=your-api-key
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
AzureOpenAI__DeploymentName=gpt-4
AzureOpenAI__ApiVersion=2024-02-01

# Azure Search (if using)
AzureSearch__ApiKey=your-search-api-key
AzureSearch__ServiceName=your-search-service
AzureSearch__IndexName=nlweb-index
```

## Kubernetes Deployment

### Basic Kubernetes Deployment

1. **Apply configuration:**
```bash
kubectl apply -f deployment/kubernetes/configmap.yaml
kubectl apply -f deployment/kubernetes/secrets-template.yaml  # Update with real secrets
kubectl apply -f deployment/kubernetes/deployment.yaml
kubectl apply -f deployment/kubernetes/service.yaml
kubectl apply -f deployment/kubernetes/ingress.yaml
```

2. **Update secrets with real values:**
```bash
# Create secrets manually with real values
kubectl create secret generic nlwebnet-secrets \
  --from-literal=azure-openai-api-key=your-actual-key \
  --from-literal=azure-search-api-key=your-actual-search-key \
  --from-literal=openai-api-key=your-actual-openai-key
```

3. **Verify deployment:**
```bash
kubectl get pods -l app=nlwebnet-demo
kubectl get svc nlwebnet-demo-service
kubectl logs -l app=nlwebnet-demo
```

### Scaling

```bash
# Scale horizontally
kubectl scale deployment nlwebnet-demo --replicas=5

# Check status
kubectl get hpa
```

## Azure Deployment

### Azure Container Apps

1. **Deploy using ARM template:**
```bash
az group create --name nlwebnet-rg --location eastus

az deployment group create \
  --resource-group nlwebnet-rg \
  --template-file deployment/azure/container-apps.json \
  --parameters \
    containerAppName=nlwebnet-demo \
    containerImage=your-registry.azurecr.io/nlwebnet-demo:latest \
    azureOpenAIApiKey=your-api-key \
    azureOpenAIEndpoint=https://your-resource.openai.azure.com/ \
    azureSearchApiKey=your-search-key \
    azureSearchServiceName=your-search-service
```

2. **Get the application URL:**
```bash
az containerapp show \
  --name nlwebnet-demo \
  --resource-group nlwebnet-rg \
  --query properties.configuration.ingress.fqdn
```

### Azure App Service

1. **Deploy using ARM template:**
```bash
az deployment group create \
  --resource-group nlwebnet-rg \
  --template-file deployment/azure/app-service.json \
  --parameters \
    webAppName=nlwebnet-demo-app \
    dockerImage=your-registry.azurecr.io/nlwebnet-demo:latest \
    azureOpenAIApiKey=your-api-key \
    azureOpenAIEndpoint=https://your-resource.openai.azure.com/ \
    azureSearchApiKey=your-search-key \
    azureSearchServiceName=your-search-service
```

### Azure Container Registry

```bash
# Create ACR
az acr create --name yourregistry --resource-group nlwebnet-rg --sku Basic

# Build and push image
az acr build --registry yourregistry --image nlwebnet-demo:latest .
```

## Helm Deployment

### Installing with Helm

1. **Install the chart:**
```bash
helm install nlwebnet-demo ./deployment/helm/nlwebnet-demo \
  --set image.repository=your-registry.azurecr.io/nlwebnet-demo \
  --set image.tag=latest \
  --set config.azureOpenAI.endpoint=https://your-resource.openai.azure.com/ \
  --set config.azureSearch.serviceName=your-search-service \
  --set secrets.azureOpenAIApiKey=your-api-key \
  --set secrets.azureSearchApiKey=your-search-key \
  --set ingress.hosts[0].host=nlwebnet-demo.yourdomain.com
```

2. **Upgrade deployment:**
```bash
helm upgrade nlwebnet-demo ./deployment/helm/nlwebnet-demo \
  --set image.tag=v1.1.0
```

3. **Uninstall:**
```bash
helm uninstall nlwebnet-demo
```

### Customizing Helm Values

Create a custom `values.yaml` file:

```yaml
# custom-values.yaml
image:
  repository: your-registry.azurecr.io/nlwebnet-demo
  tag: v1.0.0

ingress:
  enabled: true
  hosts:
    - host: nlwebnet-demo.yourdomain.com
      paths:
        - path: /
          pathType: Prefix

config:
  azureOpenAI:
    endpoint: https://your-resource.openai.azure.com/
  azureSearch:
    serviceName: your-search-service

secrets:
  azureOpenAIApiKey: your-api-key
  azureSearchApiKey: your-search-key

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 20
```

Deploy with custom values:
```bash
helm install nlwebnet-demo ./deployment/helm/nlwebnet-demo -f custom-values.yaml
```

## Environment Configuration

### Development
- Use Docker Compose with override for hot reload
- Enable detailed logging
- Use development certificates

### Staging
- Production-like configuration
- Reduced resource limits
- Automated testing integration

### Production
- Enable auto-scaling
- Configure monitoring and alerting
- Use proper secrets management
- Enable HTTPS and security headers

## Monitoring and Health Checks

### Health Endpoint

The application exposes a health check endpoint at `/health`:

```bash
curl http://your-app-url/health
# Response: {"status":"healthy","timestamp":"2024-01-01T12:00:00.000Z"}
```

### Kubernetes Health Checks

The application includes both liveness and readiness probes:

- **Liveness Probe:** `/health` every 30 seconds
- **Readiness Probe:** `/health` every 10 seconds

### Monitoring Integration

For production deployments, integrate with:

- **Azure Application Insights** (for Azure deployments)
- **Prometheus + Grafana** (for Kubernetes)
- **Container Insights** (for Azure Container Apps)

### Logging

Application logs include:
- Request/response logging
- Health check status
- AI service integration logs
- Performance metrics

Access logs:
```bash
# Kubernetes
kubectl logs -l app=nlwebnet-demo

# Docker
docker logs container-name

# Azure Container Apps
az containerapp logs show --name nlwebnet-demo --resource-group nlwebnet-rg
```

## Troubleshooting

### Common Issues

1. **Health Check Failures:**
   - Verify the application is listening on port 8080
   - Check environment variables are correctly set
   - Ensure dependencies (AI services) are accessible

2. **Image Build Issues:**
   - Verify .NET 9.0 SDK availability
   - Check network connectivity for NuGet restore
   - Review .dockerignore for excluded files

3. **Deployment Failures:**
   - Validate Kubernetes manifests: `kubectl apply --dry-run=client`
   - Check resource quotas and limits
   - Verify secrets and config maps are applied

### Getting Help

- Check application logs for detailed error messages
- Verify configuration values match your environment
- Test connectivity to external dependencies (AI services, search)
- Use health endpoints to diagnose issues

For additional support, refer to the main README.md file and the project's GitHub issues.