# Multi-stage Dockerfile for NLWebNet Demo Application
# Based on .NET 9 runtime and optimized for production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and project files for dependency resolution
COPY ["NLWebNet.sln", "./"]
COPY ["src/NLWebNet/NLWebNet.csproj", "src/NLWebNet/"]
COPY ["samples/Demo/NLWebNet.Demo.csproj", "samples/Demo/"]
COPY ["samples/AspireHost/NLWebNet.AspireHost.csproj", "samples/AspireHost/"]
COPY ["tests/NLWebNet.Tests/NLWebNet.Tests.csproj", "tests/NLWebNet.Tests/"]

# Restore dependencies
RUN dotnet restore "samples/Demo/NLWebNet.Demo.csproj"

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