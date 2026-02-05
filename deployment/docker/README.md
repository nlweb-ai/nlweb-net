# Docker Build Guide for NLWebNet

This document provides guidance for building Docker containers for the NLWebNet Demo application, including workarounds for common SSL issues in certain environments.

## Quick Start

### GitHub Actions / CI Environments (Recommended)

For CI/CD environments with proper SSL configuration, the Docker build works automatically:

```bash
docker build -f deployment/docker/Dockerfile -t nlwebnet-demo:latest .
```

This approach is used in the GitHub Actions workflow and publishes images to `ghcr.io/nlweb-ai/nlweb-net/demo`.

### Local Development (SSL Issues)

For local development environments that experience SSL certificate validation errors, use the pre-built approach:

```bash
# Pre-restore packages locally (bypasses SSL issues)
dotnet restore

# Use the fallback Dockerfile that skips network operations
docker build -f deployment/docker/Dockerfile.local -t nlwebnet-demo:latest .
```

Or use the automated build script:

```bash
./deployment/docker/build-docker.sh
```

## Build Approaches

### 1. Standard Dockerfile (CI/CD)

**File**: `deployment/docker/Dockerfile`

- Restores NuGet packages during Docker build
- Optimized for CI/CD environments with proper SSL
- Uses multi-stage build for minimal final image
- Includes comprehensive SSL certificate handling

**Status**: ✅ Works in GitHub Actions, ❌ May fail in local environments with SSL issues

### 2. Local Development Dockerfile

**File**: `deployment/docker/Dockerfile.local`

- Uses pre-restored packages from host
- Bypasses SSL issues during Docker build
- Requires `dotnet restore` to be run before building
- Suitable for environments with network restrictions

**Status**: ✅ Works in environments with SSL certificate issues

## SSL Certificate Issues

### Problem

In some Docker environments, you may encounter SSL certificate validation errors during `dotnet restore`:

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
The SSL connection could not be established, see inner exception.
The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot
```

This typically occurs in:
- Corporate environments with custom SSL certificates
- Development environments with outdated certificate stores
- Containers running in restricted network environments

### Solution

1. **For Production**: Use the GitHub Actions CI/CD pipeline which builds and publishes images to GHCR
2. **For Local Development**: Use the pre-restore approach with `Dockerfile.local`
3. **For Testing**: Pull pre-built images from `ghcr.io/nlweb-ai/nlweb-net/demo`

### Environment Variables Applied

The Dockerfile includes these SSL-related configurations:

```dockerfile
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_CERT_REVOCATION_MODE=offline  
ENV DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=false
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
```

## Testing

### Smoke Test

After building, test the container:

```bash
./deployment/docker/smoke-test.sh nlwebnet-demo:latest
```

### Manual Testing

```bash
# Start container
docker run -d --name nlwebnet-test -p 8080:8080 nlwebnet-demo:latest

# Test health endpoint
curl http://localhost:8080/health

# Clean up
docker stop nlwebnet-test && docker rm nlwebnet-test
```

## GitHub Container Registry (GHCR)

Pre-built images are automatically published to GHCR:

```bash
# Pull latest release
docker pull ghcr.io/nlweb-ai/nlweb-net/demo:latest

# Pull specific version
docker pull ghcr.io/nlweb-ai/nlweb-net/demo:v1.0.0

# Run from GHCR
docker run -p 8080:8080 ghcr.io/nlweb-ai/nlweb-net/demo:latest
```

## Troubleshooting

### Local SSL Issues

1. **Clear NuGet cache**: `dotnet nuget locals all --clear`
2. **Update certificates**: `update-ca-certificates` (Linux) or update Windows certificates
3. **Use pre-restore approach**: Run `dotnet restore` locally before Docker build
4. **Use pre-built images**: Pull from GHCR instead of building locally

### Build Failures

1. **Check network connectivity**: `ping api.nuget.org`
2. **Verify Docker version**: Ensure Docker is up to date
3. **Check available space**: Ensure sufficient disk space for Docker layers
4. **Review build logs**: Check for specific error messages

### Container Runtime Issues

1. **Check port availability**: Ensure port 8080 is not already in use
2. **Verify health endpoint**: Test `/health` endpoint after container starts
3. **Review application logs**: Use `docker logs <container-name>`
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