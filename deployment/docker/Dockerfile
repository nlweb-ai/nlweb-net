# Multi-stage Dockerfile for NLWebNet Demo Application
# Based on .NET 9 runtime and optimized for production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Set essential environment variables for .NET in containerized environments
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV NUGET_XMLDOC_MODE=skip
# Configure .NET to handle SSL issues in Docker environment
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV SSL_CERT_FILE=/etc/ssl/certs/ca-certificates.crt

# Install ca-certificates and update certificate store
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Copy solution file and project files for dependency resolution
COPY ["NLWebNet.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["NuGet.Config", "./"]
COPY ["src/NLWebNet/NLWebNet.csproj", "src/NLWebNet/"]
COPY ["samples/Demo/NLWebNet.Demo.csproj", "samples/Demo/"]
COPY ["tests/NLWebNet.Tests/NLWebNet.Tests.csproj", "tests/NLWebNet.Tests/"]

# Restore dependencies with retry and improved networking
RUN dotnet restore "samples/Demo/NLWebNet.Demo.csproj" \
    --verbosity normal \
    --force

# Copy source code
COPY . .

# Build the demo application
WORKDIR "/src/samples/Demo"
RUN dotnet build "NLWebNet.Demo.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NLWebNet.Demo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install curl for health checks and create a non-root user for security
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* \
    && groupadd -r nlwebnet && useradd -r -g nlwebnet nlwebnet

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

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