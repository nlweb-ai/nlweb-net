# Multi-stage Dockerfile for NLWebNet Demo Application
# Based on .NET 9.0 and optimized for production deployment

#
# Build Stage
#
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY NLWebNet.sln ./
COPY src/NLWebNet/NLWebNet.csproj src/NLWebNet/
COPY samples/Demo/NLWebNet.Demo.csproj samples/Demo/
COPY samples/AspireHost/NLWebNet.AspireHost.csproj samples/AspireHost/
COPY tests/NLWebNet.Tests/NLWebNet.Tests.csproj tests/NLWebNet.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build the solution
WORKDIR /src/samples/Demo
RUN dotnet build -c Release --no-restore

#
# Publish Stage
#
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

#
# Runtime Stage
#
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Security: Create non-root user
RUN groupadd -r nlwebnet && useradd -r -g nlwebnet nlwebnet

# Install curl for health checks (minimal footprint)
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R nlwebnet:nlwebnet /app
USER nlwebnet

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "NLWebNet.Demo.dll"]