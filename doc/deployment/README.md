# 🚀 NLWebNet Deployment Guide

This guide provides comprehensive instructions for deploying NLWebNet across various platforms and environments.

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Azure Deployment](#azure-deployment)
- [Production Considerations](#production-considerations)
- [Monitoring and Health Checks](#monitoring-and-health-checks)
- [Troubleshooting](#troubleshooting)

## Quick Start

### Prerequisites

- **.NET 9 SDK** (for building from source)
- **Docker** (for containerized deployment)
- **Azure CLI** (for Azure deployments)
- **kubectl** (for Kubernetes deployments)

### Local Development

```bash
# Clone the repository
git clone https://github.com/jongalloway/NLWebNet.git
cd NLWebNet

# Run with Docker Compose
cd deployment/docker && docker-compose up --build

# Or run locally (requires .NET 9)
cd samples/Demo
dotnet run
```

Access the application at `http://localhost:8080`

## Docker Deployment

### Building the Container

Use the provided build script for easy Docker image creation:

```bash
# Build with default settings
./deployment/scripts/deploy/build-docker.sh

# Build with specific tag
./deployment/scripts/deploy/build-docker.sh -t v1.0.0

# Build and push to registry
./deployment/scripts/deploy/build-docker.sh -t v1.0.0 -r myregistry.azurecr.io -p
```

### Manual Docker Build

```bash
# Build the image
docker build -t nlwebnet-demo:latest .

# Run the container
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e NLWebNet__DefaultMode=List \
  -e NLWebNet__EnableStreaming=true \
  nlwebnet-demo:latest
```

### Docker Compose

For local development with dependencies:

```bash
# Start all services
cd deployment/docker && docker-compose up -d

# View logs
docker-compose logs -f nlwebnet-demo

# Stop all services
docker-compose down
```

### Environment Variables

Key environment variables for Docker deployment:

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URLs |
| `NLWebNet__DefaultMode` | `List` | Default query mode |
| `NLWebNet__EnableStreaming` | `true` | Enable streaming responses |
| `AzureOpenAI__Endpoint` | - | Azure OpenAI service endpoint |
| `AzureOpenAI__ApiKey` | - | Azure OpenAI API key |
| `AzureSearch__ServiceName` | - | Azure Search service name |
| `AzureSearch__ApiKey` | - | Azure Search API key |

## Kubernetes Deployment

### Quick Deploy

```bash
# Deploy all resources
kubectl apply -f deployment/kubernetes/manifests/

# Check deployment status
kubectl get pods -l app=nlwebnet-demo
kubectl get services
kubectl get ingress
```

### Step-by-Step Deployment

1. **Create namespace** (optional):
   ```bash
   kubectl create namespace nlwebnet
   kubectl config set-context --current --namespace=nlwebnet
   ```

2. **Deploy configuration**:
   ```bash
   # Update secrets with your API keys
   kubectl create secret generic nlwebnet-secrets \
     --from-literal=azure-openai-api-key="your-key" \
     --from-literal=azure-search-api-key="your-key"
   
   # Apply configuration
   kubectl apply -f deployment/kubernetes/manifests/configmap.yaml
   ```

3. **Deploy application**:
   ```bash
   kubectl apply -f deployment/kubernetes/manifests/deployment.yaml
   kubectl apply -f deployment/kubernetes/manifests/service.yaml
   kubectl apply -f deployment/kubernetes/manifests/ingress.yaml
   ```

4. **Verify deployment**:
   ```bash
   kubectl get pods
   kubectl get services
   kubectl logs -l app=nlwebnet-demo
   ```

### Scaling

```bash
# Manual scaling
kubectl scale deployment nlwebnet-demo --replicas=5

# Auto-scaling (HPA already configured)
kubectl get hpa
```

### Access the Application

```bash
# Port forward for testing
kubectl port-forward service/nlwebnet-demo-service 8080:80

# Or via LoadBalancer
kubectl get service nlwebnet-demo-loadbalancer
```

## Azure Deployment

### Prerequisites

```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure
az login

# Set subscription (if needed)
az account set --subscription "your-subscription-id"
```

### Container Apps Deployment

```bash
# Deploy using script
./deployment/scripts/deploy/deploy-azure.sh -g myResourceGroup -t container-apps

# Or manual deployment
az deployment group create \
  --resource-group myResourceGroup \
  --template-file deployment/azure/container-apps.bicep \
  --parameters @deployment/azure/container-apps.parameters.json
```

### App Service Deployment

```bash
# Deploy to App Service
./deployment/scripts/deploy/deploy-azure.sh -g myResourceGroup -t app-service -e prod

# Manual deployment
az deployment group create \
  --resource-group myResourceGroup \
  --template-file deployment/azure/app-service.bicep \
  --parameters appName=nlwebnet environment=prod
```

### Azure Kubernetes Service (AKS)

1. **Create AKS cluster**:
   ```bash
   az aks create \
     --resource-group myResourceGroup \
     --name nlwebnet-aks \
     --node-count 3 \
     --enable-addons http_application_routing
   ```

2. **Get credentials**:
   ```bash
   az aks get-credentials --resource-group myResourceGroup --name nlwebnet-aks
   ```

3. **Deploy application**:
   ```bash
   kubectl apply -f deployment/kubernetes/manifests/
   ```

## Production Considerations

### Security

- **Use HTTPS**: Configure TLS certificates
- **Secrets Management**: Use Azure Key Vault or Kubernetes secrets
- **Network Policies**: Implement network segmentation
- **RBAC**: Configure proper access controls
- **Container Security**: Scan images for vulnerabilities

### Performance

- **Resource Limits**: Set appropriate CPU/memory limits
- **Auto-scaling**: Configure HPA for Kubernetes or scale rules for Azure
- **Caching**: Implement Redis caching if needed
- **CDN**: Use Azure Front Door or similar for static assets

### Monitoring

- **Application Insights**: Enabled by default in Azure deployments
- **Health Checks**: Available at `/health` and `/health/detailed`
- **Logging**: Structured logging with correlation IDs
- **Metrics**: OpenTelemetry integration available

### Backup and Recovery

- **Configuration Backup**: Store configuration in version control
- **Database Backup**: If using external databases
- **Disaster Recovery**: Multi-region deployment for critical applications

## Monitoring and Health Checks

### Health Check Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Basic health status |
| `/health/detailed` | Detailed component health |

### Example Health Check

```bash
# Basic health check
curl http://localhost:8080/health

# Detailed health check
curl http://localhost:8080/health/detailed
```

### Monitoring Integration

- **Azure Monitor**: Automatic integration in Azure deployments
- **Prometheus**: Metrics endpoint available for Kubernetes
- **Application Insights**: Telemetry and performance monitoring

## Troubleshooting

### Common Issues

1. **Container won't start**:
   ```bash
   # Check logs
   docker logs nlwebnet-demo
   kubectl logs -l app=nlwebnet-demo
   ```

2. **Health check failing**:
   ```bash
   # Test health endpoint
   curl -v http://localhost:8080/health
   ```

3. **Performance issues**:
   ```bash
   # Check resource usage
   kubectl top pods
   kubectl describe pod <pod-name>
   ```

### Debug Commands

```bash
# Docker debugging
docker exec -it nlwebnet-demo /bin/bash

# Kubernetes debugging
kubectl exec -it <pod-name> -- /bin/bash
kubectl describe pod <pod-name>
kubectl get events --sort-by=.metadata.creationTimestamp
```

### Logs

```bash
# Docker logs
cd deployment/docker && docker-compose logs -f nlwebnet-demo

# Kubernetes logs
kubectl logs -f -l app=nlwebnet-demo
kubectl logs -f deployment/nlwebnet-demo

# Azure Container Apps logs
az containerapp logs show --name nlwebnet-dev --resource-group myResourceGroup
```

## Support

For deployment issues:

1. Check the [GitHub Issues](https://github.com/jongalloway/NLWebNet/issues)
2. Review the application logs
3. Verify configuration settings
4. Test health endpoints
5. Check resource quotas and limits

---

**Note**: This is experimental software and not recommended for production use without thorough testing and evaluation.