# NLWebNet Helm Chart

This Helm chart deploys NLWebNet to a Kubernetes cluster.

## Prerequisites

- Kubernetes 1.19+
- Helm 3.0+

## Installing the Chart

```bash
# Add the chart repository (when available)
# helm repo add nlwebnet https://charts.nlwebnet.io
# helm repo update

# Install from local files
cd helm
helm install nlwebnet ./nlwebnet

# Install with custom values
helm install nlwebnet ./nlwebnet -f my-values.yaml

# Install with inline values
helm install nlwebnet ./nlwebnet \
  --set image.repository=myregistry.azurecr.io/nlwebnet-demo \
  --set image.tag=v1.0.0 \
  --set ingress.enabled=true \
  --set ingress.hosts[0].host=nlwebnet.example.com
```

## Configuration

### Basic Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `image.repository` | Container image repository | `nlwebnet-demo` |
| `image.tag` | Container image tag | `latest` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `replicaCount` | Number of replicas | `2` |
| `service.type` | Kubernetes service type | `ClusterIP` |
| `service.port` | Service port | `80` |

### Application Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `app.environment` | ASP.NET Core environment | `Production` |
| `app.nlwebnet.defaultMode` | Default query mode | `List` |
| `app.nlwebnet.enableStreaming` | Enable streaming responses | `true` |
| `app.azureOpenAI.endpoint` | Azure OpenAI endpoint | `""` |
| `app.azureOpenAI.deploymentName` | Azure OpenAI deployment | `gpt-4` |

### Secrets Configuration

Create secrets separately for security:

```bash
# Create secrets manually
kubectl create secret generic nlwebnet-secrets \
  --from-literal=azure-openai-api-key="your-azure-openai-key" \
  --from-literal=azure-search-api-key="your-azure-search-key" \
  --from-literal=openai-api-key="your-openai-key"

# Use existing secrets
helm install nlwebnet ./nlwebnet \
  --set secrets.useExisting=true \
  --set secrets.existingSecretName=nlwebnet-secrets
```

### Ingress Configuration

```bash
# Enable ingress with NGINX
helm install nlwebnet ./nlwebnet \
  --set ingress.enabled=true \
  --set ingress.className=nginx \
  --set ingress.hosts[0].host=nlwebnet.example.com \
  --set ingress.hosts[0].paths[0].path=/ \
  --set ingress.hosts[0].paths[0].pathType=Prefix
```

### Auto-scaling Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `autoscaling.enabled` | Enable horizontal pod autoscaler | `true` |
| `autoscaling.minReplicas` | Minimum number of replicas | `2` |
| `autoscaling.maxReplicas` | Maximum number of replicas | `10` |
| `autoscaling.targetCPUUtilizationPercentage` | Target CPU utilization | `70` |
| `autoscaling.targetMemoryUtilizationPercentage` | Target memory utilization | `80` |

## Examples

### Development Environment

```yaml
# dev-values.yaml
app:
  environment: Development
  nlwebnet:
    defaultMode: List
    enableStreaming: true

ingress:
  enabled: true
  hosts:
    - host: nlwebnet-dev.local
      paths:
        - path: /
          pathType: Prefix

autoscaling:
  enabled: false

replicaCount: 1

resources:
  requests:
    cpu: 50m
    memory: 128Mi
  limits:
    cpu: 200m
    memory: 256Mi
```

```bash
helm install nlwebnet-dev ./nlwebnet -f dev-values.yaml
```

### Production Environment

```yaml
# prod-values.yaml
app:
  environment: Production
  azureOpenAI:
    endpoint: "https://your-resource.openai.azure.com/"
    deploymentName: "gpt-4"
  azureSearch:
    serviceName: "your-search-service"
    indexName: "nlweb-index"

ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: nlwebnet.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: nlwebnet-tls
      hosts:
        - nlwebnet.example.com

secrets:
  useExisting: true
  existingSecretName: nlwebnet-secrets

autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 20

resources:
  requests:
    cpu: 100m
    memory: 256Mi
  limits:
    cpu: 500m
    memory: 512Mi
```

```bash
helm install nlwebnet-prod ./nlwebnet -f prod-values.yaml
```

## Uninstalling the Chart

```bash
helm uninstall nlwebnet
```

## Upgrading the Chart

```bash
# Upgrade with new values
helm upgrade nlwebnet ./nlwebnet -f new-values.yaml

# Upgrade to new chart version
helm upgrade nlwebnet ./nlwebnet --version 0.2.0
```

## Health Checks

The chart includes health checks that monitor:
- Application health at `/health`
- Detailed component health at `/health/detailed`

## Monitoring

Integration with monitoring systems:
- Prometheus metrics (when enabled)
- Application Insights (for Azure deployments)
- OpenTelemetry support

## Troubleshooting

```bash
# Check pod status
kubectl get pods -l app.kubernetes.io/name=nlwebnet

# View pod logs
kubectl logs -l app.kubernetes.io/name=nlwebnet

# Check service
kubectl get svc nlwebnet

# Test health endpoint
kubectl port-forward svc/nlwebnet 8080:80
curl http://localhost:8080/health
```