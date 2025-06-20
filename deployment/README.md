# NLWebNet Deployment Scripts

This directory contains deployment scripts and examples for various platforms.

## Usage

### Quick Docker Build and Test
```bash
./scripts/docker-build-and-test.sh
```

### Deploy to Azure Container Apps
```bash
./scripts/deploy-azure-container-apps.sh
```

### Deploy to Kubernetes
```bash
./scripts/deploy-kubernetes.sh
```

## Files

- `docker-build-and-test.sh` - Build Docker image and run basic tests
- `deploy-azure-container-apps.sh` - Deploy to Azure Container Apps
- `deploy-kubernetes.sh` - Deploy to Kubernetes cluster
- `deploy-helm.sh` - Deploy using Helm chart

All scripts include proper error handling and validation.