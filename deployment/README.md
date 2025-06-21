# NLWebNet Deployment Guide

This guide covers deployment strategies for NLWebNet across different environments and platforms.

## Quick Start

### Local Development with Docker

1. **Build and run with Docker Compose:**
   ```bash
   # Basic setup
   docker-compose up -d

   # With monitoring stack
   docker-compose --profile monitoring up -d

   # With reverse proxy
   docker-compose --profile with-proxy up -d
   ```

2. **Access the application:**
   - Application: http://localhost:8080
   - Health checks: http://localhost:8080/health
   - OpenAPI docs: http://localhost:8080/openapi/v1.json

## Container Deployment

### Docker Build

```bash
# Build the Docker image
docker build -t nlwebnet:latest .

# Run the container
docker run -d \
  --name nlwebnet \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  nlwebnet:latest
```

### Kubernetes Deployment

1. **Apply Kubernetes manifests:**
   ```bash
   # Apply all manifests
   kubectl apply -f deployment/kubernetes/

   # Or apply individually
   kubectl apply -f deployment/kubernetes/configmap.yaml
   kubectl apply -f deployment/kubernetes/deployment.yaml
   kubectl apply -f deployment/kubernetes/service.yaml
   kubectl apply -f deployment/kubernetes/ingress.yaml
   kubectl apply -f deployment/kubernetes/hpa.yaml
   ```

2. **Update configuration:**
   - Edit `deployment/kubernetes/configmap.yaml` for application settings
   - Edit `deployment/kubernetes/configmap.yaml` secrets section for API keys
   - Update ingress hostname in `deployment/kubernetes/ingress.yaml`

3. **Scale the deployment:**
   ```bash
   kubectl scale deployment nlwebnet --replicas=5
   ```

## Cloud Platform Deployment

### Azure Container Apps

See [Azure Container Apps configuration](azure/container-apps.json) for deployment templates.

### Azure App Service

See [Azure App Service configuration](azure/app-service.json) for deployment templates.

### Azure Kubernetes Service (AKS)

```bash
# Connect to your AKS cluster
az aks get-credentials --resource-group myResourceGroup --name myAKSCluster

# Deploy to AKS
kubectl apply -f deployment/kubernetes/
```

## Configuration

### Environment Variables

Key configuration options:

```bash
# Core NLWebNet settings
ASPNETCORE_ENVIRONMENT=Production
NLWebNet__DefaultMode=List
NLWebNet__EnableStreaming=true
NLWebNet__RateLimiting__RequestsPerWindow=1000

# AI Service Configuration
AzureOpenAI__ApiKey=your-api-key
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
OpenAI__ApiKey=your-openai-api-key

# Data Backend Configuration
AzureSearch__ApiKey=your-search-api-key
AzureSearch__ServiceName=your-search-service
```

### Secrets Management

For production deployments:

- **Azure**: Use Azure Key Vault
- **Kubernetes**: Use Kubernetes Secrets
- **Docker**: Use Docker secrets or external secret management

### Health Checks

The application includes comprehensive health checks:

- **Basic health**: `/health`
- **Detailed health**: `/health/detailed`
- **Ready check**: `/health/ready`

## Security Considerations

### Container Security

- ✅ Non-root user execution
- ✅ Minimal base image (aspnet runtime)
- ✅ Security context constraints
- ✅ Read-only root filesystem capability
- ✅ Dropped capabilities

### Network Security

- Configure network policies for Kubernetes
- Use TLS/SSL certificates (Let's Encrypt via cert-manager)
- Implement proper CORS policies
- Consider service mesh (Istio, Linkerd) for advanced scenarios

## Monitoring and Observability

### Built-in Features

- Structured logging with correlation IDs
- Prometheus metrics export
- Health check endpoints
- Rate limiting with monitoring

### Integration Options

- **Prometheus + Grafana**: Use provided Docker Compose monitoring stack
- **Azure Application Insights**: Built-in integration available
- **OpenTelemetry**: Full distributed tracing support

## Scaling and Performance

### Horizontal Pod Autoscaler (HPA)

The included HPA configuration scales based on:
- CPU utilization (target: 70%)
- Memory utilization (target: 80%)
- Min replicas: 3, Max replicas: 10

### Resource Recommendations

**Development:**
- Memory: 256Mi request, 512Mi limit
- CPU: 250m request, 500m limit

**Production:**
- Memory: 512Mi request, 1Gi limit
- CPU: 500m request, 1000m limit

## Troubleshooting

### Common Issues

1. **Container fails to start:**
   ```bash
   docker logs nlwebnet
   kubectl logs deployment/nlwebnet
   ```

2. **Health checks failing:**
   ```bash
   curl http://localhost:8080/health
   kubectl describe pod <pod-name>
   ```

3. **Configuration issues:**
   ```bash
   kubectl get configmap nlwebnet-config -o yaml
   kubectl get secret nlwebnet-secrets -o yaml
   ```

### Debugging

Enable detailed logging:
```bash
export Logging__LogLevel__NLWebNet=Debug
export NLWebNet__EnableDetailedLogging=true
```

## Production Checklist

- [ ] Configure proper resource limits
- [ ] Set up monitoring and alerting
- [ ] Configure backup and disaster recovery
- [ ] Implement CI/CD pipeline
- [ ] Set up SSL/TLS certificates
- [ ] Configure secrets management
- [ ] Review security policies
- [ ] Test auto-scaling configuration
- [ ] Verify health check endpoints
- [ ] Configure log aggregation