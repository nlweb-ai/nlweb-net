# Multi-stage Dockerfile for NLWebNet Demo Application
# Based on .NET 9 runtime and optimized for production deployment

# Multi-stage Dockerfile for NLWebNet Demo Application
# Optimized for production deployment with SSL workarounds

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install necessary certificates and tools
RUN apt-get update && \
    apt-get install -y ca-certificates curl && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables for .NET in containerized environments
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_EnableWriteXorExecute=0
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

# Copy project files first
COPY ["NLWebNet.sln", "./"]
COPY ["src/NLWebNet/NLWebNet.csproj", "src/NLWebNet/"]
COPY ["samples/Demo/NLWebNet.Demo.csproj", "samples/Demo/"]
COPY ["samples/AspireHost/NLWebNet.AspireHost.csproj", "samples/AspireHost/"]
COPY ["tests/NLWebNet.Tests/NLWebNet.Tests.csproj", "tests/NLWebNet.Tests/"]
COPY ["NuGet.Config", "./"]

# Attempt package restore with multiple fallback strategies
# This addresses the SSL certificate validation issues common in Docker environments
RUN echo "=== Attempting NuGet package restore ===" && \
    echo "Strategy 1: Standard restore" && \
    (timeout 60 dotnet restore "samples/Demo/NLWebNet.Demo.csproj" --verbosity minimal) || \
    (echo "Strategy 2: Restore with --disable-parallel" && \
     timeout 60 dotnet restore "samples/Demo/NLWebNet.Demo.csproj" --verbosity minimal --disable-parallel) || \
    (echo "Strategy 3: Clear cache and retry" && \
     dotnet nuget locals all --clear && \
     timeout 60 dotnet restore "samples/Demo/NLWebNet.Demo.csproj" --verbosity minimal --ignore-failed-sources) || \
    echo "All restore strategies failed due to SSL issues - will attempt build anyway"

# Copy all source code
COPY . .

# Build and publish with fallback strategies
# If restore failed, the build will automatically restore what it can
RUN cd samples/Demo && \
    echo "=== Building NLWebNet Demo Application ===" && \
    (echo "Attempting build with --no-restore..." && \
     dotnet build "NLWebNet.Demo.csproj" -c Release --no-restore --verbosity minimal) || \
    (echo "Build without restore failed, attempting with restore and shorter timeout..." && \
     timeout 90 dotnet build "NLWebNet.Demo.csproj" -c Release --verbosity minimal) || \
    (echo "Standard build failed, trying with ignore-failed-sources..." && \
     timeout 90 dotnet build "NLWebNet.Demo.csproj" -c Release --verbosity minimal -p:RestoreIgnoreFailedSources=true) && \
    echo "=== Publishing application ===" && \
    dotnet publish "NLWebNet.Demo.csproj" -c Release --no-build -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install curl for health checks and create a non-root user for security
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* \
    && groupadd -r nlwebnet && useradd -r -g nlwebnet nlwebnet

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create directory for logs and ensure proper permissions
RUN mkdir -p /app/logs && chown -R nlwebnet:nlwebnet /app

# Switch to non-root user
USER nlwebnet

# Configure ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "NLWebNet.Demo.dll"]