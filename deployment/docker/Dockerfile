# Multi-stage Dockerfile for NLWebNet Demo Application
# Based on .NET 9 runtime and optimized for production deployment

# Multi-stage Dockerfile for NLWebNet Demo Application
# Optimized to work around SSL certificate issues in containerized environments

# Build stage  
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install ca-certificates
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables for .NET
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_XMLDOC_MODE=skip

# Copy project files first for dependency resolution
COPY ["NLWebNet.sln", "./"]
COPY ["src/NLWebNet/NLWebNet.csproj", "src/NLWebNet/"]
COPY ["samples/Demo/NLWebNet.Demo.csproj", "samples/Demo/"]
COPY ["samples/AspireHost/NLWebNet.AspireHost.csproj", "samples/AspireHost/"]
COPY ["tests/NLWebNet.Tests/NLWebNet.Tests.csproj", "tests/NLWebNet.Tests/"]
COPY ["NuGet.Config", "./"]

# Try restore with fallback - if it fails, continue anyway
RUN echo "Attempting package restore..." && \
    export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0 && \
    export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 && \
    export COMPlus_DisableEncryptedStateCache=1 && \
    (dotnet restore "samples/Demo/NLWebNet.Demo.csproj" --verbosity minimal || \
     (echo "Restore failed with SSL errors, attempting workaround..." && \
      dotnet nuget locals all --clear && \
      dotnet restore "samples/Demo/NLWebNet.Demo.csproj" --verbosity minimal --ignore-failed-sources) || \
     echo "All restore attempts failed, continuing with no-restore build...")

# Copy source code
COPY . .

# Build and publish with no-restore fallback 
RUN cd samples/Demo && \
    echo "Building NLWebNet Demo application..." && \
    (dotnet build "NLWebNet.Demo.csproj" -c Release --no-restore --verbosity minimal || \
     dotnet build "NLWebNet.Demo.csproj" -c Release --verbosity minimal) && \
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