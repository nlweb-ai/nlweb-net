# Docker Build Guide for NLWebNet

This document provides guidance for building Docker containers for the NLWebNet Demo application, including workarounds for common SSL issues in certain environments.

## Quick Start

### Standard Build (GitHub Actions / CI)

For CI/CD environments with proper SSL configuration:

```bash
docker build -f deployment/docker/Dockerfile -t nlwebnet-demo:latest .
```

### Local Development Build

For local development environments that may have SSL certificate issues:

```bash
# Use the automated build script that handles SSL issues
./deployment/docker/build-docker.sh
```

## Build Approaches

### 1. Standard Dockerfile (Recommended for CI/CD)

**File**: `deployment/docker/Dockerfile`

- Restores NuGet packages during Docker build
- Optimized for CI/CD environments
- Uses multi-stage build for minimal final image
- Includes SSL certificate handling improvements

### 2. Pre-built Approach (Fallback for SSL Issues)

**File**: `deployment/docker/Dockerfile.pre-built`

- Requires packages to be restored on host before build
- Bypasses SSL issues by using `--no-restore`
- Useful for environments with network restrictions

## SSL Certificate Issues

### Problem

In some Docker environments, you may encounter SSL certificate validation errors:

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot
```

### Solutions

1. **Use GitHub Actions** (Recommended)
   - CI/CD environments typically have proper SSL configuration
   - The workflow automatically builds and publishes to GHCR

2. **Pre-restore packages locally**:
   ```bash
   dotnet restore
   docker build -f deployment/docker/Dockerfile.pre-built -t nlwebnet-demo .
   ```

3. **Use the automated build script**:
   ```bash
   ./deployment/docker/build-docker.sh
   ```

## Testing the Container

### Health Check

The container includes a health check endpoint:

```bash
# Start the container
docker run -p 8080:8080 nlwebnet-demo:latest

# Test health endpoint
curl http://localhost:8080/health
```

### Automated Smoke Test

Use the provided smoke test script:

```bash
# Run smoke test on built image
./deployment/docker/smoke-test.sh nlwebnet-demo:latest
```

## GitHub Container Registry (GHCR)

The GitHub Actions workflow automatically publishes images to GHCR:

```bash
# Pull from GHCR
docker pull ghcr.io/nlweb-ai/nlweb-net/demo:latest

# Run the image
docker run -p 8080:8080 ghcr.io/nlweb-ai/nlweb-net/demo:latest
```

## Environment Variables

Key environment variables for the container:

- `ASPNETCORE_ENVIRONMENT`: Set to `Production` by default
- `ASPNETCORE_URLS`: Set to `http://+:8080`
- `ASPNETCORE_HTTP_PORTS`: Set to `8080`

## Security

- Container runs as non-root user (`nlwebnet`)
- Uses minimal ASP.NET Core runtime image
- Includes security best practices

## Troubleshooting

### Build Issues

1. **SSL Certificate Errors**: Use pre-built approach or build in CI
2. **Package Restore Fails**: Ensure internet connectivity and DNS resolution
3. **Permission Denied**: Check Docker daemon permissions

### Runtime Issues

1. **Container won't start**: Check logs with `docker logs <container-name>`
2. **Health check fails**: Verify port 8080 is accessible
3. **Application errors**: Check environment variables and configuration

## Contributing

When making changes to Docker configuration:

1. Test both Dockerfile approaches
2. Update this documentation
3. Verify the smoke test passes
4. Ensure GitHub Actions workflow succeeds